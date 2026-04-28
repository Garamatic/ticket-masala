namespace TicketMasala.Domain.Events;

/// <summary>
/// Raised when tickets are grouped under a parent ticket.
/// </summary>
public record TicketGroupedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    /// <summary>
    /// The unique identifier of the parent ticket.
    /// </summary>
    public Guid ParentTicketGuid { get; }

    /// <summary>
    /// The unique identifiers of the child tickets that were grouped.
    /// </summary>
    public IReadOnlyList<Guid> ChildTicketGuids { get; }

    /// <summary>
    /// The ID of the user who performed the grouping.
    /// </summary>
    public string GroupedByUserId { get; }

    public TicketGroupedEvent(Guid parentTicketGuid, IReadOnlyList<Guid> childTicketGuids, string groupedByUserId)
    {
        ParentTicketGuid = parentTicketGuid;
        ChildTicketGuids = childTicketGuids;
        GroupedByUserId = groupedByUserId;
    }
}
