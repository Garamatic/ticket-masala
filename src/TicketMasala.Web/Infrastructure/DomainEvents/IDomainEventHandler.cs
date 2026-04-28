using TicketMasala.Domain.Events;

namespace TicketMasala.Web.Infrastructure.DomainEvents;

/// <summary>
/// Interface for handlers that process specific domain event types.
/// Implement this to react to domain events (e.g., send notifications, update caches).
/// </summary>
/// <typeparam name="TEvent">The type of domain event this handler processes.</typeparam>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the domain event.
    /// </summary>
    /// <param name="event">The domain event to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// Non-generic marker interface for domain event handlers (used for registration/discovery).
/// </summary>
public interface IDomainEventHandler
{
}
