using Microsoft.AspNetCore.Mvc.Razor;

namespace TicketMasala.Web.Tenancy;

/// <summary>
/// Expands view locations to check tenant-specific views first.
/// Falls back to default views if tenant view doesn't exist.
/// </summary>
public class TenantViewLocationExpander : IViewLocationExpander
{
    private const string TenantKey = "tenant";
    
    /// <summary>
    /// Populate the tenant value from current context.
    /// </summary>
    public void PopulateValues(ViewLocationExpanderContext context)
    {
        var tenant = GetCurrentTenant(context.ActionContext.HttpContext);
        if (!string.IsNullOrEmpty(tenant))
        {
            context.Values[TenantKey] = tenant;
        }
    }

    /// <summary>
    /// Expand view locations to include tenant-specific paths.
    /// </summary>
    public IEnumerable<string> ExpandViewLocations(
        ViewLocationExpanderContext context, 
        IEnumerable<string> viewLocations)
    {
        if (context.Values.TryGetValue(TenantKey, out var tenant) && !string.IsNullOrEmpty(tenant))
        {
            // Tenant-specific views take priority
            yield return $"/tenants/{tenant}/Views/{{1}}/{{0}}.cshtml";
            yield return $"/tenants/{tenant}/Views/Shared/{{0}}.cshtml";
        }
        
        // Fall back to default view locations
        foreach (var location in viewLocations)
        {
            yield return location;
        }
    }
    
    /// <summary>
    /// Get the current tenant from the HTTP context.
    /// Priority: Header > Query > Environment Variable
    /// </summary>
    private static string? GetCurrentTenant(HttpContext? httpContext)
    {
        if (httpContext == null) return null;
        
        // 1. Check X-Tenant header
        if (httpContext.Request.Headers.TryGetValue("X-Tenant", out var headerValue))
        {
            return headerValue.FirstOrDefault();
        }
        
        // 2. Check query string
        if (httpContext.Request.Query.TryGetValue("tenant", out var queryValue))
        {
            return queryValue.FirstOrDefault();
        }
        
        // 3. Fall back to environment variable
        return Environment.GetEnvironmentVariable("MASALA_TENANT");
    }
}
