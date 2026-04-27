using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TicketMasala.Web.Services;

/// <summary>
/// Centralized service for parsing JSON custom fields to eliminate duplication.
/// Replaces scattered JSON parsing logic across multiple services.
/// </summary>
public interface IJsonParsingService
{
    /// <summary>
    /// Parses JSON string into a dictionary of field names to values.
    /// Returns empty dictionary if JSON is null, empty, or invalid.
    /// </summary>
    Dictionary<string, object?> ParseCustomFields(string? json);

    /// <summary>
    /// Attempts to parse JSON string into a dictionary.
    /// Returns true if parsing succeeded, false otherwise.
    /// </summary>
    bool TryParseCustomFields(string? json, out Dictionary<string, object?> result);

    /// <summary>
    /// Extracts a single value from JSON by key.
    /// </summary>
    T? GetValue<T>(string? json, string key);
}

public class JsonParsingService : IJsonParsingService
{
    private readonly ILogger<JsonParsingService> _logger;

    public JsonParsingService(ILogger<JsonParsingService> logger)
    {
        _logger = logger;
    }

    public Dictionary<string, object?> ParseCustomFields(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, object?>();

        try
        {
            var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            return result?.ToDictionary(
                kvp => kvp.Key,
                kvp => ConvertJsonElement(kvp.Value)
            ) ?? new Dictionary<string, object?>();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse custom fields JSON: {Json}", json);
            return new Dictionary<string, object?>();
        }
    }

    public bool TryParseCustomFields(string? json, out Dictionary<string, object?> result)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            result = new Dictionary<string, object?>();
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            result = parsed?.ToDictionary(
                kvp => kvp.Key,
                kvp => ConvertJsonElement(kvp.Value)
            ) ?? new Dictionary<string, object?>();
            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse JSON: {Json}", json);
            result = new Dictionary<string, object?>();
            return false;
        }
    }

    public T? GetValue<T>(string? json, string key)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(key, out var prop))
            {
                return JsonSerializer.Deserialize<T>(prop.GetRawText());
            }
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to extract value for key {Key} from JSON", key);
        }

        return default;
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => element.GetRawText(),
            JsonValueKind.Array => element.GetRawText(),
            _ => element.GetRawText()
        };
    }
}
