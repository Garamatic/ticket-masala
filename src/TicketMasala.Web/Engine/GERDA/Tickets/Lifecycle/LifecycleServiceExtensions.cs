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
        // Port adapter: existing RabbitMQ publisher → IEventPublisher port
        services.AddScoped<IEventPublisher>(sp =>
        {
            var rabbitMq = sp.GetService<IRabbitMqPublisher>();
            return new RabbitMqEventPublisher(rabbitMq);
        });

        // Deep module
        services.AddScoped<ITicketLifecycle, TicketLifecycle>();

        return services;
    }
}
