namespace TicketMasala.Web.Middleware;

/// <summary>
/// Middleware that adds Deprecation and Link headers to legacy API endpoints.
/// </summary>
public class DeprecationHeaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Dictionary<string, string> _deprecatedRoutes = new()
    {
        // Legacy route -> New V1 route
        { "/api/tickets", "/api/v1/work-items" },
        { "/api/projects", "/api/v1/work-containers" }
    };

    public DeprecationHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();

        if (path != null && IsDeprecatedRoute(path, out var newRoute))
        {
            // Standard Deprecation header (draft-ietf-httpapi-deprecation-header)
            // Value can be a date or boolean "true"
            context.Response.Headers["Deprecation"] = "true";

            // Link header pointing to the new resource
            // Format: <https://api.example.com/v1/work-items>; rel="alternate"
            context.Response.Headers["Link"] = $"<{newRoute}>; rel=\"alternate\"";

            // Additionally, add specific warning header for visibility
            context.Response.Headers["X-API-Deprecation-Warning"] = $"This endpoint is deprecated. Please migrate to {newRoute}";
        }

        await _next(context);
    }

    private bool IsDeprecatedRoute(string path, out string newRoute)
    {
        // Check for exact match or starting match (for sub-resources)
        foreach (var route in _deprecatedRoutes)
        {
            if (path.StartsWith(route.Key))
            {
                newRoute = route.Value;
                return true;
            }
        }

        newRoute = string.Empty;
        return false;
    }
}
