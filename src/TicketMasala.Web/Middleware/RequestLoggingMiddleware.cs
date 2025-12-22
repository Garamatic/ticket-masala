using System.Diagnostics;

namespace TicketMasala.Web.Middleware;

/// <summary>
/// Middleware to log HTTP request details, status codes, and execution time.
/// Helps diagnose improved error handling and identify slow requests (potential 499 sources).
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var request = context.Request;

        // Log start of request (debug level to avoid noise)
        _logger.LogDebug("Request started: {Method} {Path}", request.Method, request.Path);

        try
        {
            await _next(context);

            sw.Stop();
            var statusCode = context.Response.StatusCode;

            // Log warning for 4xx errors (except 404 which is common) or slow requests (> 500ms)
            if (statusCode >= 400 && statusCode != 404)
            {
                _logger.LogWarning(
                    "Request completed with error: {Method} {Path} responded {StatusCode} in {Elapsed:0.000}ms",
                    request.Method, request.Path, statusCode, sw.Elapsed.TotalMilliseconds);
            }
            else if (sw.ElapsedMilliseconds > 500)
            {
                _logger.LogWarning(
                    "Slow request detected: {Method} {Path} responded {StatusCode} in {Elapsed:0.000}ms",
                    request.Method, request.Path, statusCode, sw.Elapsed.TotalMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "Request completed: {Method} {Path} responded {StatusCode} in {Elapsed:0.000}ms",
                    request.Method, request.Path, statusCode, sw.Elapsed.TotalMilliseconds);
            }

            // Specifically log 499 (Client Closed Request) if it happens (though usually it shows as cancellation)
            if (statusCode == 499)
            {
                _logger.LogWarning("Client closed request (499) for {Method} {Path}", request.Method, request.Path);
            }
        }
        catch (OperationCanceledException)
        {
            // This catches the cancellation when client disconnects (often results in 499 if handled by server)
            sw.Stop();
            _logger.LogWarning(
                "Request cancelled (499 Client Closed): {Method} {Path} cancelled after {Elapsed:0.000}ms",
                request.Method, request.Path, sw.Elapsed.TotalMilliseconds);

            // Re-throw to let the framework handle the response (or middleware higher up)
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "Request failed with exception: {Method} {Path} failed in {Elapsed:0.000}ms",
                request.Method, request.Path, sw.Elapsed.TotalMilliseconds);
            throw;
        }
    }
}
