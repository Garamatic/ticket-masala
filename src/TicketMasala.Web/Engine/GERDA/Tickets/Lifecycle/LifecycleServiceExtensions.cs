using TicketMasala.Web.Messaging;

namespace TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;

/// <summary>
/// DI registration for the ticket lifecycle deep module.
/// </summary>
public static class LifecycleServiceExtensions
{
    /// <summary>
    /// Register the ITicketLifecycle deep module and its port adapters.
    /// </summary>
    public static IServiceCollection AddTicketLifecycle(this IServiceCollection services)
    {
        // Deep module: events are now queued to the Outbox table atomically
        // within the same DbContext transaction. The OutboxPublisher background
        // service drains them to RabbitMQ. No direct IEventPublisher needed.
        services.AddScoped<ITicketLifecycle, TicketLifecycle>();

        return services;
    }
}
