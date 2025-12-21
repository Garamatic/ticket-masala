namespace TicketMasala.Web.Middleware;

/// <summary>
/// Middleware that generates or propagates correlation IDs for request tracking.
/// </summary>
public class CorrelationIdMiddleware
{
    /// <summary>
    /// The header name used for correlation ID.
    /// </summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>
    /// The HttpContext.Items key used to store the correlation ID.
    /// </summary>
    public const string ContextKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Processes the HTTP request and ensures a correlation ID is present.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Try to get correlation ID from request header
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();

        // Generate a new one if not provided
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        // Store in HttpContext.Items for access by other middleware/controllers
        context.Items[ContextKey] = correlationId;

        // Add to response headers
        context.Response.Headers[HeaderName] = correlationId;

        // Continue to next middleware
        await _next(context);
    }
}