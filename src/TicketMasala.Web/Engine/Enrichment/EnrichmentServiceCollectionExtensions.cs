namespace TicketMasala.Web.Engine.Enrichment;

/// <summary>
/// Extension methods to register all Enrichment module services.
/// Includes the enrichment queue and background processing service.
/// </summary>
public static class EnrichmentServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Enrichment module services to the dependency injection container.
    /// Includes the enrichment queue and background service.
    /// </summary>
    public static IServiceCollection AddEnrichmentModule(this IServiceCollection services)
    {
        services.AddSingleton<IEnrichmentQueue, EnrichmentQueue>();
        services.AddHostedService<EnrichmentBackgroundService>();

        return services;
    }
}
