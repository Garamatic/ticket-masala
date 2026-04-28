namespace TicketMasala.Web.Infrastructure.DomainEvents;

/// <summary>
/// Extension methods for registering domain event infrastructure with DI.
/// </summary>
public static class DomainEventExtensions
{
    /// <summary>
    /// Registers the domain event dispatcher and scans for handlers in the specified assemblies.
    /// </summary>
    public static IServiceCollection AddDomainEvents(this IServiceCollection services)
    {
        // Register the dispatcher
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // Scan and register all domain event handlers
        var handlerType = typeof(IDomainEventHandler);
        var handlerInterfaceType = typeof(IDomainEventHandler<>);

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.GetName().Name?.StartsWith("TicketMasala") == true);

        foreach (var assembly in assemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && handlerType.IsAssignableFrom(t));

            foreach (var type in handlerTypes)
            {
                // Find the implemented IDomainEventHandler<> interface
                var implementedInterface = type.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType);

                if (implementedInterface != null)
                {
                    services.AddTransient(implementedInterface, type);
                }
            }
        }

        return services;
    }
}
