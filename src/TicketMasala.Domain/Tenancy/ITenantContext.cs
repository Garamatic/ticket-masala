namespace TicketMasala.Domain.Tenancy;

/// <summary>
/// Provides access to the current tenant context.
/// This abstraction allows domain services to access tenant information
/// without depending on ASP.NET Core infrastructure.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant ID (e.g., "government", "healthcare").
    /// Returns null if no tenant is configured.
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// Gets the default domain ID for this tenant from configuration.
    /// </summary>
    string? DefaultDomainId { get; }

    /// <summary>
    /// Gets the display name of the current tenant.
    /// </summary>
    string? TenantDisplayName { get; }
}

/// <summary>
/// Default implementation that returns no tenant (single-tenant mode).
/// Web layer should provide a request-scoped implementation.
/// </summary>
public class DefaultTenantContext : ITenantContext
{
    public string? TenantId => null;
    public string? DefaultDomainId => "IT";
    public string? TenantDisplayName => "Default";
}
