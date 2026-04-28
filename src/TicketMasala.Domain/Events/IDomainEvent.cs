namespace TicketMasala.Domain.Events;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent significant occurrences within the domain that other parts
/// of the system may need to react to (e.g., ticket created, assigned, status changed).
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// UTC timestamp when the event occurred.
    /// </summary>
    DateTime OccurredOn { get; }
}
