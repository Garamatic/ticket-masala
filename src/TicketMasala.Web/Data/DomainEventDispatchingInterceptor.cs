using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TicketMasala.Domain.Events;
using TicketMasala.Domain.Ports;
using TicketMasala.Web.Infrastructure.DomainEvents;

using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace TicketMasala.Web.Data;

/// <summary>
/// EF Core interceptor that writes domain event outbox rows during SavingChangesAsync
/// (so they commit atomically with the aggregate) and dispatches in-process handlers
/// after a successful SavedChangesAsync.
/// </summary>
public class DomainEventDispatchingInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;
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
        if (context is null)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var events = CaptureDomainEvents(context);
        if (events.Count == 0)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        _pendingEvents[context] = events;

        var mappers = _serviceProvider.GetServices<IDomainEventContractMapper>();
        var outboxSet = context.Set<Domain.Entities.OutboxMessage>();
        var trackedIds = new HashSet<Guid>(outboxSet.Local.Select(m => m.Id));

        foreach (var @event in events)
        {
            if (trackedIds.Contains(@event.EventId))
                continue;

            var mapping = TryMapEvent(@event, mappers, _serviceProvider.GetService<ILogger<DomainEventDispatchingInterceptor>>());
            if (mapping is null)
                continue;

            var envelope = new MappedEventEnvelope(
                mapping.Payload, mapping.EventType, mapping.RoutingKey,
                @event.EventId, @event.OccurredOn);

            var outboxMessage = DomainEventPublisher.CreateOutboxMessage(envelope);
            outboxSet.Add(outboxMessage);
            trackedIds.Add(outboxMessage.Id);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null)
            return await base.SavedChangesAsync(eventData, result, cancellationToken);

        if (_pendingEvents.TryRemove(context, out var events))
        {
            var dispatcher = _serviceProvider.GetService<IDomainEventDispatcher>();
            if (dispatcher is not null)
                await dispatcher.DispatchAsync(events, cancellationToken);

            ClearEventSources(context, events);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is not null)
        {
            _pendingEvents.TryRemove(context, out _);
            var outboxEntries = context.ChangeTracker.Entries<Domain.Entities.OutboxMessage>()
                .Where(e => e.State == EntityState.Added).ToList();
            foreach (var entry in outboxEntries)
                entry.State = EntityState.Detached;
        }
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    public override async ValueTask<InterceptionResult> ThrowingConcurrencyExceptionAsync(
        ConcurrencyExceptionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            _pendingEvents.TryRemove(eventData.Context, out _);
        return await base.ThrowingConcurrencyExceptionAsync(eventData, result, cancellationToken);
    }

    private static List<IDomainEvent> CaptureDomainEvents(DbContext context)
    {
        var events = new List<IDomainEvent>();
        foreach (var entry in context.ChangeTracker.Entries<IHasDomainEvents>())
        {
            var entityEvents = entry.Entity.DomainEvents;
            if (entityEvents.Any())
                events.AddRange(entityEvents);
        }
        return events;
    }

    private static void ClearEventSources(DbContext context, List<IDomainEvent> events)
    {
        var eventIds = new HashSet<Guid>(events.Select(e => e.EventId));
        foreach (var entry in context.ChangeTracker.Entries<IHasDomainEvents>())
            if (entry.Entity.DomainEvents.Any(e => eventIds.Contains(e.EventId)))
                entry.Entity.ClearDomainEvents();
    }

    private static IntegrationEventMapping? TryMapEvent(
        IDomainEvent @event,
        IEnumerable<IDomainEventContractMapper> mappers,
        ILogger<DomainEventDispatchingInterceptor>? logger = null)
    {
        foreach (var mapper in mappers)
        {
            try
            {
                var result = mapper.Map(@event);
                if (result is not null) return result;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex,
                    "Contract mapper {MapperType} failed for event {EventType} ({EventId}). " +
                    "Skipping this mapper and continuing with next.",
                    mapper.GetType().Name, @event.GetType().Name, @event.EventId);
            }
        }
        return null;
    }
}

internal sealed class MappedEventEnvelope : IDomainEventEnvelope
{
    public MappedEventEnvelope(
        object payload, string eventType, string routingKey,
        Guid eventId, DateTime occurredOn)
    {
        Payload = payload; EventType = eventType; RoutingKeyOverride = routingKey;
        EventId = eventId; OccurredOn = occurredOn;
    }

    public object Payload { get; }
    public string EventType { get; }
    public string? RoutingKeyOverride { get; }
    public Guid EventId { get; }
    public DateTime OccurredOn { get; }
    public string? CorrelationId => System.Diagnostics.Activity.Current?.Id;
    public string? CausationId => null;
    public string? TenantId => null;
    public string SchemaVersion => "1";
}
