namespace TicketMasala.Web.Tenancy;

/// <summary>
/// Interface for tenant plugins that can extend Ticket Masala functionality.
/// Implement this in a separate assembly and place the DLL in the plugins directory.
/// </summary>
public interface ITenantPlugin
{
    /// <summary>
    /// Unique identifier for this tenant/plugin.
    /// </summary>
    string TenantId { get; }
    
    /// <summary>
    /// Display name for the tenant.
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// Register tenant-specific services (strategies, validators, etc.)
    /// Called during application startup.
    /// </summary>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    
    /// <summary>
    /// Configure tenant-specific middleware.
    /// Called after standard middleware is configured.
    /// </summary>
    void ConfigureMiddleware(IApplicationBuilder app, IWebHostEnvironment env);
}
