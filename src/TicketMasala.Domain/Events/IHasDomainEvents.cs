namespace TicketMasala.Domain.Events;

/// <summary>
/// Interface for aggregate roots that emit domain events.
/// Entities implementing this interface maintain a collection of domain events
/// that are raised during business operations. These events should be dispatched
/// after the aggregate is persisted.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// Read-only collection of domain events that have been raised but not yet dispatched.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all pending domain events after they have been dispatched.
    /// This should be called by the infrastructure/persistence layer after saving.
    /// </summary>
    void ClearDomainEvents();
}
