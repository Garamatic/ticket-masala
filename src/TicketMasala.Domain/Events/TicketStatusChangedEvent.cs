using TicketMasala.Domain.Common;

namespace TicketMasala.Domain.Events;

/// <summary>
/// Raised when a ticket's status changes.
/// </summary>
public record TicketStatusChangedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    /// <summary>
    /// The unique identifier of the ticket.
    /// </summary>
    public Guid TicketGuid { get; }

    /// <summary>
    /// The previous status of the ticket.
    /// </summary>
    public Status OldStatus { get; }

    /// <summary>
    /// The new status of the ticket.
    /// </summary>
    public Status NewStatus { get; }

    /// <summary>
    /// The ID of the user who changed the status.
    /// </summary>
    public string ChangedByUserId { get; }

    public TicketStatusChangedEvent(Guid ticketGuid, Status oldStatus, Status newStatus, string changedByUserId)
    {
        TicketGuid = ticketGuid;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ChangedByUserId = changedByUserId;
    }
}
