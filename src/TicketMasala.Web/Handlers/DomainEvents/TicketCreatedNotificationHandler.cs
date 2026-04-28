using TicketMasala.Domain.Events;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Infrastructure.DomainEvents;

namespace TicketMasala.Web.Handlers.DomainEvents;

/// <summary>
/// Example domain event handler that sends a notification when a ticket is created.
/// This demonstrates how to react to domain events.
/// </summary>
public class TicketCreatedNotificationHandler : IDomainEventHandler<TicketCreatedEvent>
{
    private readonly ILogger<TicketCreatedNotificationHandler> _logger;
    private readonly INotificationService _notificationService;

    public TicketCreatedNotificationHandler(
        ILogger<TicketCreatedNotificationHandler> logger,
        INotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task HandleAsync(TicketCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Ticket {TicketGuid} created by customer {CustomerId} in domain {DomainId}",
            @event.TicketGuid,
            @event.CustomerId,
            @event.DomainId);

        // Example: Send a notification to the customer confirming ticket creation
        // In production, this could queue a background job or send real-time notifications
        try
        {
            await _notificationService.NotifyUserAsync(
                @event.CustomerId,
                $"Your ticket has been created (ID: {@event.TicketGuid:N}). We'll get back to you shortly!",
                linkUrl: $"/Tickets/Details/{@event.TicketGuid}",
                type: "Success");
        }
        catch (Exception ex)
        {
            // Log but don't throw - domain event handlers should not fail the main transaction
            _logger.LogError(ex, "Failed to send notification for ticket {TicketGuid}", @event.TicketGuid);
        }
    }
}
