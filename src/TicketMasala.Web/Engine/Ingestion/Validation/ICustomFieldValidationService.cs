using TicketMasala.Web.Models.Configuration;

namespace TicketMasala.Web.Engine.Ingestion.Validation;

/// <summary>
/// Validates custom field JSON data against domain configuration schema.
/// </summary>
public interface ICustomFieldValidationService
{
    /// <summary>
    /// Validates custom fields JSON against the domain configuration.
    /// </summary>
    /// <param name="domainId">The domain ID to validate against</param>
    /// <param name="workItemTypeCode">The work item type code</param>
    /// <param name="customFieldsJson">JSON string containing custom field values</param>
    /// <returns>Validation result with any errors</returns>
    CustomFieldValidationResult Validate(string domainId, string? workItemTypeCode, string? customFieldsJson);
    
    /// <summary>
    /// Parses custom fields JSON into a dictionary for display.
    /// </summary>
    Dictionary<string, object?> ParseCustomFields(string? customFieldsJson);
    
    /// <summary>
    /// Serializes custom field values to JSON.
    /// </summary>
    string SerializeCustomFields(Dictionary<string, object?> values);
}

/// <summary>
/// Result of custom field validation.
/// </summary>
public class CustomFieldValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<CustomFieldError> Errors { get; set; } = new();
}

/// <summary>
/// Individual field validation error.
/// </summary>
public class CustomFieldError
{
    public required string FieldName { get; set; }
    public required string Message { get; set; }

}
