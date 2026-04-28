using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TicketMasala.Domain.Events;
using TicketMasala.Web.Infrastructure.DomainEvents;

namespace TicketMasala.Web.Data;

/// <summary>
/// EF Core interceptor that dispatches domain events after SaveChanges completes.
/// This ensures domain events are only dispatched when the transaction commits successfully.
/// </summary>
public class DomainEventDispatchingInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    // Store events per-context before save, dispatch after save
    private readonly Dictionary<DbContext, List<IDomainEvent>> _pendingEvents = new();

    public DomainEventDispatchingInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context != null)
        {
            // Collect domain events from all aggregates before saving
            var events = context.ChangeTracker.Entries<IHasDomainEvents>()
                .SelectMany(e => e.Entity.DomainEvents)
                .ToList();

            if (events.Any())
            {
                lock (_pendingEvents)
                {
                    _pendingEvents[context] = events;
                }
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context != null)
        {
            List<IDomainEvent>? events = null;
            lock (_pendingEvents)
            {
                if (_pendingEvents.TryGetValue(context, out events))
                {
                    _pendingEvents.Remove(context);
                }
            }

            if (events != null && events.Any())
            {
                // Get dispatcher from the service provider
                var dispatcher = _serviceProvider.GetService<IDomainEventDispatcher>();
                if (dispatcher != null)
                {
                    await dispatcher.DispatchAsync(events, cancellationToken);
                }

                // Clear events from aggregates
                var aggregates = context.ChangeTracker.Entries<IHasDomainEvents>()
                    .Select(e => e.Entity);

                foreach (var aggregate in aggregates)
                {
                    aggregate.ClearDomainEvents();
                }
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
