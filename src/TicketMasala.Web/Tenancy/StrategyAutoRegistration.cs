using System.Reflection;
using TicketMasala.Web.Engine.GERDA.Dispatching;
using TicketMasala.Web.Engine.GERDA.Estimating;
using TicketMasala.Web.Engine.GERDA.Ranking;

namespace TicketMasala.Web.Tenancy;

/// <summary>
/// Automatically registers strategy implementations from loaded assemblies.
/// This allows tenant plugins to provide custom ranking, dispatching, and estimating strategies.
/// </summary>
public static class StrategyAutoRegistration
{
    /// <summary>
    /// Scan assemblies and register all strategy implementations.
    /// </summary>
    public static void RegisterStrategies(IServiceCollection services, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            RegisterStrategiesFromAssembly(services, assembly);
        }
    }

    /// <summary>
    /// Scan a single assembly for strategy implementations.
    /// </summary>
    public static void RegisterStrategiesFromAssembly(IServiceCollection services, Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract);

        foreach (var type in types)
        {
            // Register ranking strategies
            if (typeof(IJobRankingStrategy).IsAssignableFrom(type))
            {
                if (!IsAlreadyRegistered(services, typeof(IJobRankingStrategy), type))
                {
                    services.AddScoped(typeof(IJobRankingStrategy), type);
                    Console.WriteLine($"[Tenancy] Registered ranking strategy: {type.Name}");
                }
            }

            // Register dispatching strategies
            if (typeof(IDispatchingStrategy).IsAssignableFrom(type))
            {
                if (!IsAlreadyRegistered(services, typeof(IDispatchingStrategy), type))
                {
                    services.AddScoped(typeof(IDispatchingStrategy), type);
                    Console.WriteLine($"[Tenancy] Registered dispatching strategy: {type.Name}");
                }
            }

            // Register estimating strategies
            if (typeof(IEstimatingStrategy).IsAssignableFrom(type))
            {
                if (!IsAlreadyRegistered(services, typeof(IEstimatingStrategy), type))
                {
                    services.AddScoped(typeof(IEstimatingStrategy), type);
                    Console.WriteLine($"[Tenancy] Registered estimating strategy: {type.Name}");
                }
            }
        }
    }

    /// <summary>
    /// Check if a specific implementation is already registered.
    /// </summary>
    private static bool IsAlreadyRegistered(IServiceCollection services, Type serviceType, Type implementationType)
    {
        return services.Any(s =>
            s.ServiceType == serviceType &&
            s.ImplementationType == implementationType);
    }

    /// <summary>
    /// Scan plugin assemblies that were loaded by TenantPluginLoader.
    /// </summary>
    public static void RegisterPluginStrategies(IServiceCollection services)
    {
        // Get assemblies from loaded plugins
        var pluginAssemblies = TenantPluginLoader.LoadedPlugins
            .Select(p => p.GetType().Assembly)
            .Distinct();

        foreach (var assembly in pluginAssemblies)
        {
            RegisterStrategiesFromAssembly(services, assembly);
        }
    }
}
