namespace TicketMasala.Web.Tenancy;

/// <summary>
/// Resolves database connection strings per tenant.
/// Allows each tenant to have its own isolated database.
/// </summary>
public class TenantConnectionResolver
{
    private readonly IConfiguration _configuration;

    public TenantConnectionResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Get the connection string for a specific tenant.
    /// Falls back to DefaultConnection if no tenant-specific connection is configured.
    /// </summary>
    public string GetConnectionString(string? tenantId = null)
    {
        if (!string.IsNullOrEmpty(tenantId))
        {
            // Check for tenant-specific connection string
            var tenantConnection = _configuration[$"Tenants:{tenantId}:ConnectionString"];
            if (!string.IsNullOrEmpty(tenantConnection))
            {
                return tenantConnection;
            }
        }

        // Fall back to default connection
        return _configuration.GetConnectionString("DefaultConnection")
               ?? "Data Source=masala.db";
    }

    /// <summary>
    /// Get the current tenant ID from environment.
    /// </summary>
    public static string? GetCurrentTenantId()
    {
        return Environment.GetEnvironmentVariable("MASALA_TENANT");
    }

    /// <summary>
    /// Get connection string for the current tenant (from environment).
    /// </summary>
    public string GetCurrentConnectionString()
    {
        return GetConnectionString(GetCurrentTenantId());
    }
}
