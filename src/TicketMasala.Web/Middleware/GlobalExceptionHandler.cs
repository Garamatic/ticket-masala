using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;
using TicketMasala.Web.ViewModels.Api;

namespace TicketMasala.Web.Middleware;

/// <summary>
/// Global exception handler that provides consistent error responses across the API.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Attempts to handle the exception and return an appropriate error response.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the exception was handled, false otherwise.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Get correlation ID from context
        var correlationId = httpContext.Items[CorrelationIdMiddleware.ContextKey]?.ToString();

        // Log the exception with correlation ID
        _logger.LogError(exception, 
            "Unhandled exception occurred. CorrelationId: {CorrelationId}", 
            correlationId);

        // Map exception to error response
        var errorResponse = MapExceptionToErrorResponse(exception, correlationId);

        // Set response status code and content type
        httpContext.Response.StatusCode = GetStatusCode(exception);
        httpContext.Response.ContentType = "application/json";

        // Serialize and write response
        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await httpContext.Response.WriteAsync(json, cancellationToken);

        return true;
    }

    /// <summary>
    /// Maps an exception to an ApiErrorResponse.
    /// </summary>
    private ApiErrorResponse MapExceptionToErrorResponse(Exception exception, string? correlationId)
    {
        return exception switch
        {
            ArgumentException argEx => new ApiErrorResponse
            {
                Error = "VALIDATION_ERROR",
                Message = argEx.Message,
                CorrelationId = correlationId
            },
            KeyNotFoundException notFoundEx => new ApiErrorResponse
            {
                Error = "NOT_FOUND",
                Message = notFoundEx.Message,
                CorrelationId = correlationId
            },
            UnauthorizedAccessException => new ApiErrorResponse
            {
                Error = "UNAUTHORIZED",
                Message = "Authentication is required to access this resource.",
                CorrelationId = correlationId
            },
            InvalidOperationException invalidOpEx => new ApiErrorResponse
            {
                Error = "INVALID_OPERATION",
                Message = invalidOpEx.Message,
                CorrelationId = correlationId
            },
            _ => new ApiErrorResponse
            {
                Error = "INTERNAL_ERROR",
                Message = "An unexpected error occurred. Please try again later.",
                CorrelationId = correlationId
                // Note: Stack traces and internal details are intentionally NOT included
            }
        };
    }

    /// <summary>
    /// Determines the appropriate HTTP status code for an exception.
    /// </summary>
    private int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}