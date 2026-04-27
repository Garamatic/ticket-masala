using System.Text.Json;

namespace TicketMasala.Web.Engine.Compiler;

/// <summary>
/// Static helper for extracting values from JSON logic blobs.
/// Used by the RuleCompilerService expression trees.
/// 
/// IMPROVEMENT: These methods parse JSON on every call. For performance-critical
/// scenarios with multiple field accesses, consider using CachedFieldExtractor
/// or IJsonParsingService which parses once and caches the JsonDocument.
/// </summary>
public static class FieldExtractor
{
    public static double GetNumber(string? json, string key)
    {
        if (string.IsNullOrEmpty(json))
            return 0;
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
        if (string.IsNullOrEmpty(json))
            return null;
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
        if (string.IsNullOrEmpty(json))
            return false;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(key, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.True)
                    return true;
                if (prop.ValueKind == JsonValueKind.False)
                    return false;

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

/// <summary>
/// Performance-optimized field extractor that caches the parsed JsonDocument.
/// Use this when you need to extract multiple fields from the same JSON string.
/// 
/// Example:
/// <code>
/// using var extractor = new CachedFieldExtractor(ticket.CustomFieldsJson);
/// var priority = extractor.GetNumber("priority");
/// var category = extractor.GetString("category");
/// var urgent = extractor.GetBool("is_urgent");
/// </code>
/// </summary>
public class CachedFieldExtractor : IDisposable
{
    private JsonDocument? _doc;
    private bool _disposed;

    public CachedFieldExtractor(string? json)
    {
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                _doc = JsonDocument.Parse(json);
            }
            catch (JsonException)
            {
                // Invalid JSON, leave _doc as null
            }
        }
    }

    public double GetNumber(string key)
    {
        if (_doc?.RootElement.TryGetProperty(key, out var prop) == true
            && prop.ValueKind == JsonValueKind.Number)
        {
            return prop.GetDouble();
        }
        return 0;
    }

    public string? GetString(string key)
    {
        if (_doc?.RootElement.TryGetProperty(key, out var prop) == true)
        {
            return prop.ToString();
        }
        return null;
    }

    public bool GetBool(string key)
    {
        if (_doc?.RootElement.TryGetProperty(key, out var prop) == true)
        {
            if (prop.ValueKind == JsonValueKind.True)
                return true;
            if (prop.ValueKind == JsonValueKind.False)
                return false;

            if (prop.ValueKind == JsonValueKind.String)
            {
                return bool.TryParse(prop.GetString(), out var result) && result;
            }
        }
        return false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _doc?.Dispose();
        _disposed = true;
    }
}
