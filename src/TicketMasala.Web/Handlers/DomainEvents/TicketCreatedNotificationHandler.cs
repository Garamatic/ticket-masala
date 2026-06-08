using TicketMasala.Domain.Events;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.Ingestion.Background;
using TicketMasala.Web.Infrastructure.DomainEvents;

namespace TicketMasala.Web.Handlers.DomainEvents;

/// <summary>
/// Domain event handler that queues a notification when a ticket is created.
/// Runs in the background so slow email services do not block the HTTP response.
/// </summary>
public class TicketCreatedNotificationHandler : IDomainEventHandler<TicketCreatedEvent>
{
    private readonly ILogger<TicketCreatedNotificationHandler> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBackgroundTaskQueue _taskQueue;

    public TicketCreatedNotificationHandler(
        ILogger<TicketCreatedNotificationHandler> logger,
        IServiceScopeFactory scopeFactory,
        IBackgroundTaskQueue taskQueue)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _taskQueue = taskQueue;
    }

    public async Task HandleAsync(TicketCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Ticket {TicketGuid} created by customer {CustomerId} in domain {DomainId}. Queuing notification...",
            @event.TicketGuid,
            @event.CustomerId,
            @event.DomainId);

        // Queue notification to background so email SMTP timeouts don't block the HTTP response
        await _taskQueue.QueueBackgroundWorkItemAsync(async ct =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                await notificationService.NotifyUserAsync(
                    @event.CustomerId,
                    $"Your ticket has been created (ID: {@event.TicketGuid:N}). We'll get back to you shortly!",
                    linkUrl: $"/Tickets/Details/{@event.TicketGuid}",
                    type: "Success");
            }
            catch (Exception ex)
            {
                // Log but don't throw - background notification failures should not break the main transaction
                _logger.LogError(ex, "Failed to send notification for ticket {TicketGuid}", @event.TicketGuid);
            }
        });
    }
}
