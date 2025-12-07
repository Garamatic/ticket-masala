namespace TicketMasala.Web.Middleware;

/// <summary>
/// Middleware that adds a correlation ID to each request for distributed tracing.
/// The correlation ID is propagated via the X-Correlation-ID header and added to HttpContext.Items.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    public const string CorrelationIdHeader = "X-Correlation-ID";
    public const string CorrelationIdKey = "CorrelationId";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get or create correlation ID
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N")[..12]; // Short format for readability
        
        // Store in HttpContext.Items for access throughout request
        context.Items[CorrelationIdKey] = correlationId;
        
        // Add to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // Extract WorkItem ID (ticket) from route if available
        string? workItemId = null;
        if (context.Request.RouteValues.TryGetValue("id", out var id))
        {
            workItemId = id?.ToString();
            context.Items["WorkItemId"] = workItemId;
        }

        // Log with correlation context using UEM property names
        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = context.User?.Identity?.Name,
            ["WorkItem.Id"] = workItemId,       // UEM: Ticket ID
            ["WorkContainer.Id"] = context.Request.Query["projectId"].FirstOrDefault()  // UEM: Project ID
        }))
        {
            await _next(context);
        }
    }
}

/// <summary>
/// Extension methods for registering CorrelationIdMiddleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
    
    /// <summary>
    /// Gets the correlation ID from the current HttpContext
    /// </summary>
    public static string? GetCorrelationId(this HttpContext context)
    {
        return context.Items[CorrelationIdMiddleware.CorrelationIdKey]?.ToString();
    }
}
