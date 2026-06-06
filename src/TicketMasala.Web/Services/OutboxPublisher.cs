using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using RabbitMqConnector;
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
    // ═════════════════════════════════════════════════════════════════════════════
    // OpenTelemetry Tracing & Metrics
    // ═════════════════════════════════════════════════════════════════════════════
    private static readonly ActivitySource ActivitySource = new("TicketMasala.Outbox");
    private static readonly Meter Meter = new("TicketMasala.Outbox", "1.0.0");

    // Counters
    private static readonly Counter<long> MessagesProcessedCounter =
        Meter.CreateCounter<long>("outbox.messages.processed", "messages", "Total messages processed");
    private static readonly Counter<long> MessagesRetriedCounter =
        Meter.CreateCounter<long>("outbox.messages.retried", "messages", "Total message retry attempts");
    private static readonly Counter<long> MessagesDeadLetteredCounter =
        Meter.CreateCounter<long>("outbox.messages.dead_lettered", "messages", "Messages moved to dead letter");

    // Histograms
    private static readonly Histogram<double> ProcessingDurationHistogram =
        Meter.CreateHistogram<double>("outbox.processing.duration_ms", "ms", "Message processing duration");
    private static readonly Histogram<int> RetryCountHistogram =
        Meter.CreateHistogram<int>("outbox.retry.count", "retries", "Distribution of retry counts before success/failure");

    // Observable gauges (for current state) — use long for Interlocked compatibility
    private long _currentOutboxDepth = 0;

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

        // Create observable gauge for outbox depth (thread-safe read via Interlocked)
        Meter.CreateObservableGauge("outbox.depth", () => Interlocked.Read(ref _currentOutboxDepth), "messages", "Current number of pending outbox messages");
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
        using var activity = ActivitySource.StartActivity("Outbox.ProcessBatch", ActivityKind.Internal);
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MasalaDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

        // Get total outbox depth (for observable gauge) — thread-safe update
        var depth = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .CountAsync(cancellationToken);
        Interlocked.Exchange(ref _currentOutboxDepth, depth);

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

        activity?.SetTag("batch.size", pendingMessages.Count);
        activity?.SetTag("outbox.depth", _currentOutboxDepth);

        _logger.LogDebug("Processing {Count} pending outbox messages", pendingMessages.Count);

        // Use a transaction to ensure all updates in this batch are atomic
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var message in pendingMessages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ProcessSingleMessageAsync(message, publisher, cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task ProcessSingleMessageAsync(
        OutboxMessage message,
        IRabbitMqPublisher publisher,
        CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("Outbox.ProcessMessage", ActivityKind.Internal);
        activity?.SetTag("message.id", message.Id);
        activity?.SetTag("message.type", message.EventType);
        activity?.SetTag("message.routing_key", message.RoutingKey);
        activity?.SetTag("message.retry_count", message.RetryCount);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await PublishMessageAsync(publisher, message, cancellationToken);

            // Mark as processed
            message.ProcessedAt = DateTime.UtcNow;
            message.Error = null;
            message.ScheduledRetryAt = null;

            stopwatch.Stop();

            // Record metrics
            MessagesProcessedCounter.Add(1,
                new KeyValuePair<string, object?>("event_type", message.EventType),
                new KeyValuePair<string, object?>("result", "success"));
            ProcessingDurationHistogram.Record(stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("event_type", message.EventType),
                new KeyValuePair<string, object?>("result", "success"));
            RetryCountHistogram.Record(message.RetryCount,
                new KeyValuePair<string, object?>("event_type", message.EventType),
                new KeyValuePair<string, object?>("result", "success"));

            activity?.SetTag("result", "success");
            activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);

            _logger.LogInformation(
                "Published outbox message {MessageId} of type {EventType} to {RoutingKey}",
                message.Id,
                message.EventType,
                message.RoutingKey);
        }
        catch (JsonException ex)
        {
            stopwatch.Stop();

            // Permanent failure: malformed JSON won't be fixed by retrying
            message.RetryCount = _options.MaxRetries;
            message.Error = $"PERMANENT: Malformed JSON payload - {ex.Message}"[..500];
            message.ScheduledRetryAt = null;

            MessagesDeadLetteredCounter.Add(1,
                new KeyValuePair<string, object?>("event_type", message.EventType),
                new KeyValuePair<string, object?>("reason", "malformed_json"));

            activity?.SetTag("error", true);
            activity?.SetTag("error.type", "JsonException");
            activity?.SetTag("error.permanent", true);

            _logger.LogError(ex,
                "Outbox message {MessageId} has malformed JSON and will be abandoned",
                message.Id);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Transient failure: may succeed on retry (if under max retries)
            message.RetryCount++;
            message.Error = ex.Message[..Math.Min(ex.Message.Length, 500)];

            // Only schedule retry if we haven't exceeded max retries
            if (message.RetryCount < _options.MaxRetries)
            {
                message.ScheduledRetryAt = DateTime.UtcNow.Add(_options.RetryDelay);
            }
            else
            {
                message.ScheduledRetryAt = null; // No more retries scheduled
            }

            MessagesRetriedCounter.Add(1,
                new KeyValuePair<string, object?>("event_type", message.EventType),
                new KeyValuePair<string, object?>("retry_count", message.RetryCount));

            activity?.SetTag("error", true);
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.transient", true);
            activity?.SetTag("retry_count", message.RetryCount);

            var isPermanent = message.RetryCount >= _options.MaxRetries;

            _logger.LogError(ex,
                "Failed to publish outbox message {MessageId} (attempt {RetryCount}/{MaxRetries}). {Result}",
                message.Id,
                message.RetryCount,
                _options.MaxRetries,
                isPermanent ? "Message will be abandoned." : "Will retry later.");

            if (isPermanent)
            {
                MessagesDeadLetteredCounter.Add(1,
                    new KeyValuePair<string, object?>("event_type", message.EventType),
                    new KeyValuePair<string, object?>("reason", "max_retries_exceeded"));

                activity?.SetTag("error.permanent", true);
                activity?.SetTag("error.reason", "max_retries_exceeded");

                _logger.LogError(
                    "Outbox message {MessageId} exceeded max retries and will be abandoned",
                    message.Id);
            }
        }
    }

    private static async Task PublishMessageAsync(
        RabbitMqConnector.IRabbitMqPublisher publisher,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        // Validate payload is valid JSON before attempting to publish
        using var document = JsonDocument.Parse(message.Payload);

        // Propagate correlation ID from outbox message to current activity
        if (!string.IsNullOrEmpty(message.CorrelationId) && Activity.Current is not null)
        {
            Activity.Current.SetTag("correlation.id", message.CorrelationId);
        }

        // Add event_type to the payload so consumers (e.g. mailing service) can identify the event
        var payload = JsonNode.Parse(document.RootElement.GetRawText());
        if (payload is JsonObject payloadObj)
        {
            payloadObj["event_type"] = message.EventType;
        }

        await publisher.PublishAsync(payload, message.RoutingKey, cancellationToken);
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
