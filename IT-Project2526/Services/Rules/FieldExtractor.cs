using System.Text.Json;

namespace IT_Project2526.Services.Rules;

/// <summary>
/// Static helper for extracting values from JSON logic blobs.
/// Used by the RuleCompilerService expression trees.
/// </summary>
public static class FieldExtractor
{
    public static double GetNumber(string? json, string key)
    {
        if (string.IsNullOrEmpty(json)) return 0;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.Number)
            {
                return prop.GetDouble();
            }
        }
        catch (JsonException)
        {
            // Ignore malformed JSON, return default
        }
        return 0;
    }

    public static string? GetString(string? json, string key)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(key, out var prop))
            {
                return prop.ToString();
            }
        }
        catch (JsonException)
        {
            // Ignore malformed JSON
        }
        return null;
    }

    public static bool GetBool(string? json, string key)
    {
        if (string.IsNullOrEmpty(json)) return false;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(key, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.True) return true;
                if (prop.ValueKind == JsonValueKind.False) return false;
                
                // Fallback: Check if string "true"
                if (prop.ValueKind == JsonValueKind.String)
                {
                    return bool.TryParse(prop.GetString(), out var result) && result;
                }
            }
        }
        catch (JsonException)
        {
            // Ignore malformed JSON
        }
        return false;
    }
}
