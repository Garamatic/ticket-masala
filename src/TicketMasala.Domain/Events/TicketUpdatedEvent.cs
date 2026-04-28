namespace TicketMasala.Domain.Events;

/// <summary>
/// Raised when a ticket's properties are updated.
/// </summary>
public record TicketUpdatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    /// <summary>
    /// The unique identifier of the ticket.
    /// </summary>
    public Guid TicketGuid { get; }

    /// <summary>
    /// The name of the property that was updated (e.g., "Description", "Title").
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// The ID of the user who performed the update.
    /// </summary>
    public string UpdatedByUserId { get; }

    public TicketUpdatedEvent(Guid ticketGuid, string propertyName, string updatedByUserId)
    {
        TicketGuid = ticketGuid;
        PropertyName = propertyName;
        UpdatedByUserId = updatedByUserId;
    }
}
