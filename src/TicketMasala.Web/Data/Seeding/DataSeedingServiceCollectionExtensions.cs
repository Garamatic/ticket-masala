namespace TicketMasala.Web.Data.Seeding;

/// <summary>
/// Extension methods to register all data seeding services.
/// Uses Strategy Pattern for seed strategies executed in registration order.
/// </summary>
public static class DataSeedingServiceCollectionExtensions
{
    /// <summary>
    /// Adds all data seeding services to the dependency injection container.
    /// Seed strategies are executed in the order they are registered.
    /// </summary>
    public static IServiceCollection AddDataSeeding(this IServiceCollection services)
    {
        // Seed Strategies (Strategy Pattern - executed in registration order)
        services.AddScoped<ISeedStrategy, RoleSeedStrategy>();
        services.AddScoped<ISeedStrategy, UserSeedStrategy>();
        services.AddScoped<ISeedStrategy, ProjectSeedStrategy>();
        services.AddScoped<ISeedStrategy, WorkItemSeedStrategy>();
        services.AddScoped<ISeedStrategy, KnowledgeBaseSeedStrategy>();

        // DbSeeder orchestrates all strategies
        services.AddScoped<DbSeeder>();

        return services;
    }
}
