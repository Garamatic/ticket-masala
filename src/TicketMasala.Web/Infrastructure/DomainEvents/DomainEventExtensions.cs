using Microsoft.Extensions.DependencyInjection.Extensions;
using TicketMasala.Domain.Ports;

namespace TicketMasala.Web.Infrastructure.DomainEvents;

/// <summary>
/// Extension methods for registering domain event infrastructure with DI.
/// </summary>
public static class DomainEventExtensions
{
    /// <summary>
    /// Registers the domain event infrastructure:
    /// <list type="bullet">
    ///   <item><see cref="IDomainEventDispatcher"/> — dispatches domain events to in-process handlers</item>
    ///   <item><see cref="IDomainEventPublisher"/> — persists domain events as outbox messages</item>
    /// <item>All IDomainEventHandler{TEvent} implementations found by assembly scan:
    ///       e.g., TicketCreatedGerdaHandler, TicketAssignedLogHandler</item>
    ///   <item>All IDomainEventContractMapper implementations found by assembly scan:
    ///       e.g., TicketResolvedContractMapper</item>
    /// </list>
    ///
    /// The transactional outbox write happens inside DomainEventDispatchingInterceptor.SavingChangesAsync,
    /// before the DB save commits. IDomainEventContractMapper implementations transform
    /// domain events into integration contracts (e.g. RabbitMqConnector.Contracts types) at that point.
    /// When no mapper exists for an event type, the event is skipped (no outbox row).
    ///
    /// In-process handlers (IDomainEventHandler{TEvent}) run after the save completes
    /// via IDomainEventDispatcher.
    /// </summary>
    public static IServiceCollection AddDomainEvents(this IServiceCollection services)
    {
        // ── 1. Domain event dispatcher (in-process handlers) ────────────────────
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // ── 2. IDomainEventPublisher — outbox bridge ─────────────────────────────
        //    DomainEventPublisher adds OutboxMessage rows to the current DbContext.
        //    The outbox rows commit atomically with the aggregate changes because
        //    the interceptor calls PublishAsync inside SavingChangesAsync.
        //    TryAdd so that callers can replace it with a test double.
        services.TryAddScoped<IDomainEventPublisher, DomainEventPublisher>();

        // ── 3. Scan and register all IDomainEventHandler<T> implementations ───────
        //    These run AFTER the DB save (in-process like GERDA, notifications).
        var handlerInterfaceType = typeof(IDomainEventHandler<>);
        var handlerAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.GetName().Name?.StartsWith("TicketMasala") == true);

        foreach (var assembly in handlerAssemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IDomainEventHandler).IsAssignableFrom(t));

            foreach (var type in handlerTypes)
            {
                if (type.IsGenericTypeDefinition)
                    continue;

                var implementedInterface = type.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType);

                if (implementedInterface != null)
                {
                    services.AddTransient(implementedInterface, type);
                }
            }
        }

        // ── 4. Scan and register all IDomainEventContractMapper implementations ──
        //    These run INSIDE SavingChangesAsync via the interceptor to map domain
        //    events to integration contracts before outbox serialization.
        var mapperType = typeof(IDomainEventContractMapper);
        var mapperAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.GetName().Name?.StartsWith("TicketMasala") == true);

        foreach (var assembly in mapperAssemblies)
        {
            var mapperTypes = assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false } && mapperType.IsAssignableFrom(t));

            foreach (var type in mapperTypes)
            {
                services.AddTransient(mapperType, type);
            }
        }

        return services;
    }
}
