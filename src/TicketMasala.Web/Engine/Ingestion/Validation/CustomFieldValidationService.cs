using System.Text.Json;
using TicketMasala.Web.Models.Configuration;
using TicketMasala.Web.Engine.GERDA.Configuration;

namespace TicketMasala.Web.Engine.Ingestion.Validation;

/// <summary>
/// Validates custom field data against domain configuration schema.
/// </summary>
public class CustomFieldValidationService : ICustomFieldValidationService
{
    private readonly IDomainConfigurationService _domainConfig;
    private readonly ILogger<CustomFieldValidationService> _logger;

    public CustomFieldValidationService(
        IDomainConfigurationService domainConfig,
        ILogger<CustomFieldValidationService> logger)
    {
        _domainConfig = domainConfig;
        _logger = logger;
    }

    public CustomFieldValidationResult Validate(string domainId, string? workItemTypeCode, string? customFieldsJson)
    {
        var result = new CustomFieldValidationResult();
        var fieldDefinitions = _domainConfig.GetCustomFields(domainId).ToList();
        var values = ParseCustomFields(customFieldsJson);

        foreach (var field in fieldDefinitions)
        {
            var hasValue = values.TryGetValue(field.Name, out var value) && !IsEmpty(value);
            
            // Check required fields
            var isRequired = field.Required || 
                (workItemTypeCode != null && field.RequiredForTypes.Contains(workItemTypeCode, StringComparer.OrdinalIgnoreCase));
            
            if (isRequired && !hasValue)
            {
                result.Errors.Add(new CustomFieldError 
                { 
                    FieldName = field.Name, 
                    Message = $"{field.Label} is required" 
                });
                continue;
            }

            if (!hasValue) continue;

            // Type-specific validation
            switch (field.Type.ToLowerInvariant())
            {
                case "number":
                case "currency":
                    if (!ValidateNumeric(field, value, result)) continue;
                    break;
                    
                case "select":
                    if (!ValidateSelect(field, value, result)) continue;
                    break;
                    
                case "multi_select":
                    if (!ValidateMultiSelect(field, value, result)) continue;
                    break;
            }
        }

        return result;
    }

    private bool ValidateNumeric(CustomFieldDefinition field, object? value, CustomFieldValidationResult result)
    {
        if (!decimal.TryParse(value?.ToString(), out var numericValue))
        {
            result.Errors.Add(new CustomFieldError 
            { 
                FieldName = field.Name, 
                Message = $"{field.Label} must be a valid number" 
            });
            return false;
        }

        if (field.Min.HasValue && numericValue < (decimal)field.Min.Value)
        {
            result.Errors.Add(new CustomFieldError 
            { 
                FieldName = field.Name, 
                Message = $"{field.Label} must be at least {field.Min}" 
            });
            return false;
        }

        if (field.Max.HasValue && numericValue > (decimal)field.Max.Value)
        {
            result.Errors.Add(new CustomFieldError 
            { 
                FieldName = field.Name, 
                Message = $"{field.Label} must be at most {field.Max}" 
            });
            return false;
        }

        return true;
    }

    private bool ValidateSelect(CustomFieldDefinition field, object? value, CustomFieldValidationResult result)
    {
        var stringValue = value?.ToString();
        if (string.IsNullOrEmpty(stringValue)) return true;

        if (!field.Options.Contains(stringValue, StringComparer.OrdinalIgnoreCase))
        {
            result.Errors.Add(new CustomFieldError 
            { 
                FieldName = field.Name, 
                Message = $"{field.Label} must be one of: {string.Join(", ", field.Options)}" 
            });
            return false;
        }

        return true;
    }

    private bool ValidateMultiSelect(CustomFieldDefinition field, object? value, CustomFieldValidationResult result)
    {
        if (value is not JsonElement jsonElement) return true;
        
        if (jsonElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in jsonElement.EnumerateArray())
            {
                var itemValue = item.GetString();
                if (!string.IsNullOrEmpty(itemValue) && 
                    !field.Options.Contains(itemValue, StringComparer.OrdinalIgnoreCase))
                {
                    result.Errors.Add(new CustomFieldError 
                    { 
                        FieldName = field.Name, 
                        Message = $"{field.Label}: '{itemValue}' is not a valid option" 
                    });
                    return false;
                }
            }
        }

        return true;
    }

    private static bool IsEmpty(object? value)
    {
        if (value == null) return true;
        if (value is string s) return string.IsNullOrWhiteSpace(s);
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind == JsonValueKind.Null || 
                   jsonElement.ValueKind == JsonValueKind.Undefined ||
                   (jsonElement.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(jsonElement.GetString()));
        }
        return false;
    }

    public Dictionary<string, object?> ParseCustomFields(string? customFieldsJson)
    {
        if (string.IsNullOrWhiteSpace(customFieldsJson))
            return new Dictionary<string, object?>();

        try
        {
            var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(customFieldsJson);
            return result?.ToDictionary(
                kvp => kvp.Key, 
                kvp => (object?)ConvertJsonElement(kvp.Value)
            ) ?? new Dictionary<string, object?>();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse custom fields JSON: {Json}", customFieldsJson);
            return new Dictionary<string, object?>();
        }
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetDecimal(out var d) ? d : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
            _ => element.GetRawText()
        };
    }

    public string SerializeCustomFields(Dictionary<string, object?> values)
    {
        return JsonSerializer.Serialize(values, new JsonSerializerOptions 
        { 
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

}
