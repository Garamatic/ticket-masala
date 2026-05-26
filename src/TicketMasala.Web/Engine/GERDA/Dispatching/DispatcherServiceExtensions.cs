using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Engine.GERDA.Dispatching;

public static class DispatcherServiceExtensions
{
    /// <summary>Registers ITicketDispatcher. Uses NoOpDispatcher when dispatching is disabled.</summary>
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
                sp.GetRequiredService<IAffinityScorer>(),
                sp.GetRequiredService<ITicketLifecycle>(),
                sp.GetRequiredService<IUnitOfWork>(),
                sp.GetRequiredService<ILogger<TicketDispatcher>>());
        });

        return services;
    }
}
