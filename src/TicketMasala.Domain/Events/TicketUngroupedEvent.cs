namespace TicketMasala.Domain.Events;

/// <summary>
/// Raised when a ticket is removed from its parent group.
/// </summary>
public record TicketUngroupedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    /// <summary>
    /// The unique identifier of the ticket that was ungrouped.
    /// </summary>
    public Guid TicketGuid { get; }

    /// <summary>
    /// The unique identifier of the former parent ticket, if any.
    /// </summary>
    public Guid? ParentTicketGuid { get; }

    /// <summary>
    /// The ID of the user who performed the ungrouping.
    /// </summary>
    public string UngroupedByUserId { get; }

    public TicketUngroupedEvent(Guid ticketGuid, Guid? parentTicketGuid, string ungroupedByUserId)
    {
        TicketGuid = ticketGuid;
        ParentTicketGuid = parentTicketGuid;
        UngroupedByUserId = ungroupedByUserId;
    }
}
