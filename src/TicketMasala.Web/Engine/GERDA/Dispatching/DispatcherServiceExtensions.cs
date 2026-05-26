using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;

namespace TicketMasala.Web.Engine.GERDA.Dispatching;

/// <summary>
/// DI registration for the ticket dispatcher deep module.
/// </summary>
public static class DispatcherServiceExtensions
{
    /// <summary>
    /// Register ITicketDispatcher and its dependencies.
    /// Uses NoOpDispatcher when dispatching is disabled in configuration.
    /// </summary>
    public static IServiceCollection AddTicketDispatcher(this IServiceCollection services)
    {
        services.AddScoped<ITicketDispatcher>(sp =>
        {
            var config = sp.GetRequiredService<GerdaConfig>();
            if (!config.GerdaAI.IsEnabled || !config.GerdaAI.Dispatching.IsEnabled)
            {
                return new NoOpDispatcher();
            }

            return new TicketDispatcher(
                sp.GetRequiredService<MasalaDbContext>(),
                config,
                sp.GetRequiredService<IAutoDispatchPolicy>(),
                sp.GetRequiredService<IAffinityScorer>(),
                sp.GetRequiredService<ITicketLifecycle>(),
                sp.GetRequiredService<ILogger<TicketDispatcher>>());
        });

        return services;
    }
}
