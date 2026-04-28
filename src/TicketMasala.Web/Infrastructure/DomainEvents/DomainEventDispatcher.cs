using TicketMasala.Domain.Events;

namespace TicketMasala.Web.Infrastructure.DomainEvents;

/// <summary>
/// Dispatches domain events to their registered handlers.
/// This is typically called after SaveChanges to ensure events are only dispatched
/// when the transaction commits successfully.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches all pending domain events from an aggregate root.
    /// </summary>
    /// <param name="aggregate">The aggregate root containing events to dispatch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DispatchAsync(IHasDomainEvents aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatches a collection of domain events.
    /// </summary>
    /// <param name="events">The events to dispatch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of IDomainEventDispatcher using dependency injection.
/// Handlers are resolved from the service provider.
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(IHasDomainEvents aggregate, CancellationToken cancellationToken = default)
    {
        var events = aggregate.DomainEvents.ToList();
        if (events.Count == 0)
            return;

        await DispatchAsync(events, cancellationToken);

        // Clear events after successful dispatch
        aggregate.ClearDomainEvents();
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var @event in events)
        {
            await DispatchSingleAsync(@event, cancellationToken);
        }
    }

    private async Task DispatchSingleAsync(IDomainEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            var eventType = @event.GetType();
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

            // Get all registered handlers for this event type
            var handlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler == null)
                    continue;

                var handleMethod = handlerType.GetMethod("HandleAsync");
                if (handleMethod != null)
                {
                    await (Task)handleMethod.Invoke(handler, new object[] { @event, cancellationToken })!;
                }
            }

            _logger.LogDebug("Dispatched domain event {EventType} with {HandlerCount} handlers",
                eventType.Name, handlers?.Count() ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching domain event {EventType}", @event.GetType().Name);
            // Don't re-throw - domain event failures shouldn't break the main transaction
            // Consider adding a dead-letter queue or retry mechanism in production
        }
    }
}
