namespace TicketMasala.Web.Engine.Security;

/// <summary>
/// Extension methods to register all Security module services.
/// Includes PII scrubbing and security-related services.
/// </summary>
public static class SecurityServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Security module services to the dependency injection container.
    /// Includes PII scrubbing and related security services.
    /// </summary>
    public static IServiceCollection AddSecurityModule(this IServiceCollection services)
    {
        services.AddScoped<IPiiScrubberService, PiiScrubberService>();

        return services;
    }
}
