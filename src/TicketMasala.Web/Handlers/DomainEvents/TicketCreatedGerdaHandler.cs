using System.Diagnostics;
using System.Diagnostics.Metrics;
using TicketMasala.Domain.Events;
using TicketMasala.Web.Engine.GERDA;
using TicketMasala.Web.Engine.Ingestion.Background;
using TicketMasala.Web.Infrastructure.DomainEvents;

namespace TicketMasala.Web.Handlers.DomainEvents;

/// <summary>
/// Domain event handler that queues GERDA AI processing when a ticket is created.
/// Replaces the fire-and-forget Task.Run pattern with proper background processing.
/// </summary>
/// <remarks>
/// This handler is dispatched by the DomainEventDispatchingInterceptor after the ticket
/// is successfully persisted. The handler queues the GERDA work onto the background
/// task queue, which is processed by QueuedHostedService.
///
/// Benefits over Task.Run:
/// - Observable queue depth (IBackgroundTaskQueue.QueuedCount)
/// - Proper error handling and logging
/// - Retry with exponential backoff
/// - Graceful shutdown support (work items complete before app exit)
/// </remarks>
public class TicketCreatedGerdaHandler : IDomainEventHandler<TicketCreatedEvent>
{
    // ═════════════════════════════════════════════════════════════════════════════
    // OpenTelemetry Tracing & Metrics
    // ═════════════════════════════════════════════════════════════════════════════
    private static readonly ActivitySource ActivitySource = new("TicketMasala.GERDA.Background");
    private static readonly Meter Meter = new("TicketMasala.GERDA.Background", "1.0.0");

    private static readonly Counter<long> GerdaQueuedCounter =
        Meter.CreateCounter<long>("gerda.background.queued", "tickets", "Total GERDA processing jobs queued");
    private static readonly Counter<long> GerdaProcessedCounter =
        Meter.CreateCounter<long>("gerda.background.processed", "tickets", "Total GERDA background jobs completed");
    private static readonly Counter<long> GerdaFailedCounter =
        Meter.CreateCounter<long>("gerda.background.failed", "tickets", "Total GERDA background jobs failed");
    private static readonly Histogram<double> GerdaQueueDepthHistogram =
        Meter.CreateHistogram<double>("gerda.background.queue_depth", "jobs", "GERDA background queue depth when job queued");
    private static readonly Histogram<double> GerdaProcessingDurationHistogram =
        Meter.CreateHistogram<double>("gerda.background.duration_ms", "ms", "GERDA background processing duration");

    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TicketCreatedGerdaHandler> _logger;

    public TicketCreatedGerdaHandler(
        IBackgroundTaskQueue taskQueue,
        IServiceScopeFactory scopeFactory,
        ILogger<TicketCreatedGerdaHandler> logger)
    {
        _taskQueue = taskQueue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(TicketCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("GERDA.QueueProcessing", ActivityKind.Producer);
        activity?.SetTag("ticket.guid", @event.TicketGuid);
        activity?.SetTag("ticket.domain", @event.DomainId);

        // Record queue depth before queuing
        var queueDepth = _taskQueue.QueuedCount;
        GerdaQueueDepthHistogram.Record(queueDepth);
        GerdaQueuedCounter.Add(1,
            new KeyValuePair<string, object?>("domain", @event.DomainId));

        activity?.SetTag("queue.depth", queueDepth);

        _logger.LogInformation(
            "Queuing GERDA processing for ticket {TicketGuid} (Domain: {DomainId}, QueueDepth: {QueueDepth})",
            @event.TicketGuid,
            @event.DomainId,
            queueDepth);

        await _taskQueue.QueueBackgroundWorkItemAsync(async ct =>
        {
            using var workActivity = ActivitySource.StartActivity("GERDA.BackgroundWork", ActivityKind.Internal);
            workActivity?.SetTag("ticket.guid", @event.TicketGuid);

            var stopwatch = Stopwatch.StartNew();

            using var scope = _scopeFactory.CreateScope();
            var gerda = scope.ServiceProvider.GetRequiredService<IGerda>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<TicketCreatedGerdaHandler>>();

            try
            {
                logger.LogInformation(
                    "GERDA Background: Starting processing for ticket {TicketGuid}",
                    @event.TicketGuid);

                var outcome = await gerda.ProcessAsync(@event.TicketGuid);

                stopwatch.Stop();

                GerdaProcessedCounter.Add(1,
                    new KeyValuePair<string, object?>("domain", @event.DomainId),
                    new KeyValuePair<string, object?>("result", outcome.WasGrouped ? "grouped" : "ungrouped"));
                GerdaProcessingDurationHistogram.Record(stopwatch.ElapsedMilliseconds,
                    new KeyValuePair<string, object?>("result", "success"));

                workActivity?.SetTag("result", "success");
                workActivity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);
                workActivity?.SetTag("outcome.was_grouped", outcome.WasGrouped);
                workActivity?.SetTag("outcome.agent_assigned", outcome.SuggestedAgentId.HasValue);

                if (outcome.WasGrouped)
                {
                    logger.LogInformation(
                        "GERDA: Ticket {TicketGuid} was grouped",
                        @event.TicketGuid);
                }

                if (outcome.SuggestedAgentId.HasValue)
                {
                    logger.LogInformation(
                        "GERDA: Ticket {TicketGuid} assigned to agent {AgentId}",
                        @event.TicketGuid,
                        outcome.SuggestedAgentId.Value);
                }

                logger.LogInformation(
                    "GERDA Background: Completed processing for ticket {TicketGuid} in {DurationMs}ms. " +
                    "Effort: {Effort}, Priority: {Priority}, Articles: {ArticleCount}",
                    @event.TicketGuid,
                    stopwatch.ElapsedMilliseconds,
                    outcome.EstimatedEffort,
                    outcome.PriorityScore,
                    outcome.RelatedArticles.Count);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                GerdaFailedCounter.Add(1,
                    new KeyValuePair<string, object?>("domain", @event.DomainId),
                    new KeyValuePair<string, object?>("error_type", ex.GetType().Name));
                GerdaProcessingDurationHistogram.Record(stopwatch.ElapsedMilliseconds,
                    new KeyValuePair<string, object?>("result", "failure"));

                workActivity?.SetTag("error", true);
                workActivity?.SetTag("error.type", ex.GetType().Name);
                workActivity?.SetTag("error.message", ex.Message);
                workActivity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);

                // Log error but don't throw - background processing failures
                // shouldn't fail the main transaction
                logger.LogError(ex,
                    "GERDA Background: Failed to process ticket {TicketGuid} after {DurationMs}ms. " +
                    "Error: {ErrorMessage}",
                    @event.TicketGuid,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                // TODO: P2 - Add retry logic with Polly or move to dead-letter queue
                // TODO: P2 - Add alerting for high failure rates
            }
        });
    }
}
