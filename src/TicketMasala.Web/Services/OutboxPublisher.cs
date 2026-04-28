using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Services;

/// <summary>
/// Configuration options for the OutboxPublisher background service.
/// </summary>
public class OutboxPublisherOptions
{
    /// <summary>
    /// How often to poll for pending messages (default: 5 seconds).
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Delay between retry attempts (default: 1 minute).
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Maximum number of retry attempts before abandoning a message (default: 3).
    /// Must be at least 1. A value of 1 means messages are tried once and never retried.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Number of messages to process in each batch (default: 10).
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// Initial delay before starting to process messages (default: 5 seconds).
    /// Allows the application to fully start up before processing begins.
    /// </summary>
    public TimeSpan StartupDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Validates that all options have valid values.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when an option has an invalid value.</exception>
    public void Validate()
    {
        if (PollInterval <= TimeSpan.Zero)
            throw new ArgumentException("PollInterval must be greater than zero.", nameof(PollInterval));

        if (RetryDelay <= TimeSpan.Zero)
            throw new ArgumentException("RetryDelay must be greater than zero.", nameof(RetryDelay));

        if (MaxRetries < 1)
            throw new ArgumentException("MaxRetries must be at least 1. Use 1 to attempt once with no retries.", nameof(MaxRetries));

        if (BatchSize < 1)
            throw new ArgumentException("BatchSize must be at least 1.", nameof(BatchSize));

        if (StartupDelay < TimeSpan.Zero)
            throw new ArgumentException("StartupDelay cannot be negative.", nameof(StartupDelay));
    }
}

/// <summary>
/// Background service that drains outbox messages to RabbitMQ.
/// Implements the Outbox Pattern for reliable event publishing.
/// </summary>
public class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisher> _logger;
    private readonly OutboxPublisherOptions _options;

    public OutboxPublisher(
        IServiceProvider serviceProvider,
        ILogger<OutboxPublisher> logger,
        OutboxPublisherOptions? options = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new OutboxPublisherOptions();
        _options.Validate();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "OutboxPublisher started. Polling every {PollInterval}s for pending messages (batch size: {BatchSize}, max retries: {MaxRetries})",
            _options.PollInterval.TotalSeconds,
            _options.BatchSize,
            _options.MaxRetries);

        // Wait for the application to fully start up
        await Task.Delay(_options.StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown, don't log as error
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_options.PollInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MasalaDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

        // Get pending messages (not processed, and eligible for retry)
        // Messages with RetryCount < MaxRetries are eligible (0-indexed, so MaxRetries=3 allows attempts at counts 0,1,2)
        var pendingMessages = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .Where(m => m.ScheduledRetryAt == null || m.ScheduledRetryAt <= DateTime.UtcNow)
            .Where(m => m.RetryCount < _options.MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Processing {Count} pending outbox messages", pendingMessages.Count);

        // Use a transaction to ensure all updates in this batch are atomic
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var message in pendingMessages)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await ProcessSingleMessageAsync(message, publisher, cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task ProcessSingleMessageAsync(
        OutboxMessage message,
        IRabbitMqPublisher publisher,
        CancellationToken cancellationToken)
    {
        try
        {
            await PublishMessageAsync(publisher, message, cancellationToken);

            // Mark as processed
            message.ProcessedAt = DateTime.UtcNow;
            message.Error = null;
            message.ScheduledRetryAt = null;

            _logger.LogInformation(
                "Published outbox message {MessageId} of type {EventType} to {RoutingKey}",
                message.Id,
                message.EventType,
                message.RoutingKey);
        }
        catch (JsonException ex)
        {
            // Permanent failure: malformed JSON won't be fixed by retrying
            message.RetryCount = _options.MaxRetries;
            message.Error = $"PERMANENT: Malformed JSON payload - {ex.Message}"[..500];
            message.ScheduledRetryAt = null;

            _logger.LogError(ex,
                "Outbox message {MessageId} has malformed JSON and will be abandoned",
                message.Id);
        }
        catch (Exception ex)
        {
            // Transient failure: may succeed on retry
            message.RetryCount++;
            message.Error = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
            message.ScheduledRetryAt = DateTime.UtcNow.Add(_options.RetryDelay);

            _logger.LogError(ex,
                "Failed to publish outbox message {MessageId} (attempt {RetryCount}/{MaxRetries})",
                message.Id,
                message.RetryCount,
                _options.MaxRetries);

            if (message.RetryCount >= _options.MaxRetries)
            {
                _logger.LogError(
                    "Outbox message {MessageId} exceeded max retries and will be abandoned",
                    message.Id);
            }
        }
    }

    private static async Task PublishMessageAsync(
        IRabbitMqPublisher publisher,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        // Validate payload is valid JSON before attempting to publish
        using var document = JsonDocument.Parse(message.Payload);

        // Pass the JsonDocument.RootElement which avoids double-serialization
        // JsonElement serializes back to the original JSON structure
        await publisher.PublishAsync(document.RootElement, message.RoutingKey, cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OutboxPublisher stopping...");

        try
        {
            await base.StopAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
    }
}
