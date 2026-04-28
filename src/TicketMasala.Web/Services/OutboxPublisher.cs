using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Services;

/// <summary>
/// Background service that drains pending outbox messages to RabbitMQ.
/// Implements the Outbox pattern for reliable event publishing.
/// </summary>
public class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisher> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _maxRetryDelay = TimeSpan.FromHours(1);
    private readonly int _maxRetryCount = 10;

    public OutboxPublisher(
        IServiceProvider serviceProvider,
        ILogger<OutboxPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisher background service started");

        // Wait a bit for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("OutboxPublisher background service stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MasalaDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

        // Get pending messages (not processed and either not scheduled or ready for retry)
        var pendingMessages = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .Where(m => m.ScheduledRetryAt == null || m.ScheduledRetryAt <= DateTime.UtcNow)
            .Where(m => m.RetryCount < _maxRetryCount)
            .OrderBy(m => m.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Processing {Count} pending outbox messages", pendingMessages.Count);

        foreach (var message in pendingMessages)
        {
            try
            {
                // Connect to RabbitMQ if not already connected
                await publisher.ConnectAsync(cancellationToken);

                // Deserialize the payload to an object for publishing
                var payload = JsonSerializer.Deserialize<object>(message.Payload);
                if (payload == null)
                {
                    throw new InvalidOperationException("Failed to deserialize outbox message payload");
                }

                // Publish to RabbitMQ
                await publisher.PublishAsync(payload, message.RoutingKey, cancellationToken);

                // Mark as processed
                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;

                _logger.LogDebug(
                    "Published outbox message {MessageId} for event {EventType}",
                    message.Id,
                    message.EventType);
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;

                // Exponential backoff with max delay
                var delay = TimeSpan.FromSeconds(Math.Min(
                    Math.Pow(2, message.RetryCount) * 10,
                    _maxRetryDelay.TotalSeconds));
                message.ScheduledRetryAt = DateTime.UtcNow.Add(delay);

                _logger.LogWarning(
                    ex,
                    "Failed to publish outbox message {MessageId} (attempt {RetryCount}/{MaxRetryCount}). " +
                    "Retry scheduled at {RetryAt}",
                    message.Id,
                    message.RetryCount,
                    _maxRetryCount,
                    message.ScheduledRetryAt);
            }
        }

        // Save changes for all processed/failed messages
        await context.SaveChangesAsync(cancellationToken);
    }
}
