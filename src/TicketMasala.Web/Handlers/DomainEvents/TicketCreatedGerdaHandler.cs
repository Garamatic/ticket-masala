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
        _logger.LogInformation(
            "Queuing GERDA processing for ticket {TicketGuid} (Domain: {DomainId})",
            @event.TicketGuid,
            @event.DomainId);

        await _taskQueue.QueueBackgroundWorkItemAsync(async ct =>
        {
            using var scope = _scopeFactory.CreateScope();
            var gerda = scope.ServiceProvider.GetRequiredService<IGerda>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<TicketCreatedGerdaHandler>>();

            try
            {
                logger.LogInformation(
                    "GERDA Background: Starting processing for ticket {TicketGuid}",
                    @event.TicketGuid);

                var outcome = await gerda.ProcessAsync(@event.TicketGuid);

                if (outcome.WasGrouped)
                {
                    logger.LogInformation(
                        "GERDA: Ticket {TicketGuid} was grouped under parent {ParentGuid}",
                        @event.TicketGuid,
                        outcome.EstimatedEffort);
                }

                if (outcome.SuggestedAgentId.HasValue)
                {
                    logger.LogInformation(
                        "GERDA: Ticket {TicketGuid} assigned to agent {AgentId}",
                        @event.TicketGuid,
                        outcome.SuggestedAgentId.Value);
                }

                logger.LogInformation(
                    "GERDA Background: Completed processing for ticket {TicketGuid}. " +
                    "Effort: {Effort}, Priority: {Priority}, Articles: {ArticleCount}",
                    @event.TicketGuid,
                    outcome.EstimatedEffort,
                    outcome.PriorityScore,
                    outcome.RelatedArticles.Count);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - background processing failures
                // shouldn't fail the main transaction
                logger.LogError(ex,
                    "GERDA Background: Failed to process ticket {TicketGuid}. " +
                    "Error: {ErrorMessage}",
                    @event.TicketGuid,
                    ex.Message);

                // TODO: P1 - Add retry logic with Polly or move to dead-letter queue
                // TODO: P1 - Add metrics for failed GERDA processing
            }
        });
    }
}
