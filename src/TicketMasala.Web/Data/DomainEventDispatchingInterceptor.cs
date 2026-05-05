using System.Collections.Concurrent;
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

    // Instance-level storage - safe because DbContext and interceptor have same (scoped) lifetime
    private readonly ConcurrentDictionary<DbContext, List<IDomainEvent>> _pendingEvents = new();

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
            // Capture events before saving - they will be dispatched after successful save
            var events = CaptureDomainEvents(context);
            if (events.Count > 0)
            {
                _pendingEvents[context] = events;
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
        if (context != null && _pendingEvents.TryRemove(context, out var events))
        {
            await DispatchEventsAsync(events, context, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<InterceptionResult> ThrowingConcurrencyExceptionAsync(
        ConcurrencyExceptionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        // Clean up pending events when a concurrency exception occurs
        var context = eventData.Context;
        if (context != null)
        {
            _pendingEvents.TryRemove(context, out _);
        }

        return await base.ThrowingConcurrencyExceptionAsync(eventData, result, cancellationToken);
    }

    private static List<IDomainEvent> CaptureDomainEvents(DbContext context)
    {
        var events = new List<IDomainEvent>();
        var aggregates = context.ChangeTracker.Entries<IHasDomainEvents>();

        foreach (var entry in aggregates)
        {
            var entityEvents = entry.Entity.DomainEvents;
            if (entityEvents.Any())
            {
                events.AddRange(entityEvents);
            }
        }

        return events;
    }

    private async Task DispatchEventsAsync(List<IDomainEvent> events, DbContext context, CancellationToken cancellationToken)
    {
        // Get dispatcher from the service provider
        var dispatcher = _serviceProvider.GetService<IDomainEventDispatcher>();
        if (dispatcher == null)
            return;

        await dispatcher.DispatchAsync(events, cancellationToken);

        // Clear events only from aggregates that had events
        var aggregates = context.ChangeTracker.Entries<IHasDomainEvents>();
        foreach (var entry in aggregates)
        {
            if (entry.Entity.DomainEvents.Any())
            {
                entry.Entity.ClearDomainEvents();
            }
        }
    }
}
