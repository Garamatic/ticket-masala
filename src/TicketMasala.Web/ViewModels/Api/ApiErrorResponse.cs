namespace TicketMasala.Web.ViewModels.Api;

/// <summary>
/// Represents a standardized API error response format.
/// </summary>
public class ApiErrorResponse
{
    /// <summary>
    /// The error code identifying the type of error.
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// A unique correlation ID for tracking the request.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Additional error details, typically field-level validation errors.
    /// </summary>
    public Dictionary<string, string[]>? Details { get; set; }
}