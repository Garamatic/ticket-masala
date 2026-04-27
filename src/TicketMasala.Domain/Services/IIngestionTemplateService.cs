namespace TicketMasala.Domain.Services;

/// <summary>
/// Service for transforming external data using templates.
/// Enables no-code ingestion mapping via configuration.
/// </summary>
public interface IIngestionTemplateService
{
    /// <summary>
    /// Transforms source data using a named ingestion template
    /// </summary>
    IngestionResult Transform(string templateName, Dictionary<string, object> sourceData);

    /// <summary>
    /// Lists available ingestion templates
    /// </summary>
    IEnumerable<string> GetTemplateNames();
}

/// <summary>
/// Result of ingestion template transformation
/// </summary>
public class IngestionResult
{
    public bool Success { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string DomainId { get; set; } = "IT";
    public string? CustomerId { get; set; }
    public Dictionary<string, object> CustomFields { get; set; } = new();
    public string? Error { get; set; }
}
