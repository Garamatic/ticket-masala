using System.Text.RegularExpressions;
using System.Web;

namespace TicketMasala.Web.Utilities;

/// <summary>
/// Utility class for sanitizing user input to prevent XSS and injection attacks
/// </summary>
public static class InputSanitizer
{
    /// <summary>
    /// Remove potentially dangerous HTML tags and scripts
    /// </summary>
    public static string SanitizeHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove script tags and their content
        input = Regex.Replace(input, @"<script[^>]*>.*?</script>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Remove iframe tags
        input = Regex.Replace(input, @"<iframe[^>]*>.*?</iframe>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Remove on* event handlers (onclick, onerror, etc.)
        input = Regex.Replace(input, @"\s*on\w+\s*=\s*[""'][^""']*[""']", string.Empty, RegexOptions.IgnoreCase);
        input = Regex.Replace(input, @"\s*on\w+\s*=\s*\w+", string.Empty, RegexOptions.IgnoreCase);

        // Remove javascript: protocol
        input = Regex.Replace(input, @"javascript\s*:", string.Empty, RegexOptions.IgnoreCase);

        // Remove data: protocol (can be used for XSS)
        input = Regex.Replace(input, @"data\s*:", string.Empty, RegexOptions.IgnoreCase);

        return input;
    }

    /// <summary>
    /// Sanitize text for safe display (encode HTML entities)
    /// </summary>
    public static string SanitizeForDisplay(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return HttpUtility.HtmlEncode(input);
    }

    /// <summary>
    /// Sanitize string to prevent SQL injection (basic validation)
    /// Note: Always use parameterized queries instead when possible
    /// </summary>
    public static string SanitizeSqlInput(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove SQL comment markers
        input = input.Replace("--", string.Empty);
        input = input.Replace("/*", string.Empty);
        input = input.Replace("*/", string.Empty);

        // Remove common SQL injection patterns
        input = Regex.Replace(input, @"('\s*(or|and)\s*'?\d)", string.Empty, RegexOptions.IgnoreCase);
        input = Regex.Replace(input, @"(;\s*(drop|alter|create|truncate|exec|execute|union|insert|update|delete))", string.Empty, RegexOptions.IgnoreCase);

        return input.Trim();
    }

    /// <summary>
    /// Validate and sanitize email addresses
    /// </summary>
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, emailPattern);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Sanitize JSON input to prevent JSON injection
    /// </summary>
    public static string SanitizeJsonInput(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove potential JSON injection characters
        input = input.Replace("\\", "\\\\");
        input = input.Replace("\"", "\\\"");
        input = input.Replace("\n", "\\n");
        input = input.Replace("\r", "\\r");
        input = input.Replace("\t", "\\t");

        return input;
    }

    /// <summary>
    /// Validate GUID format
    /// </summary>
    public static bool IsValidGuid(string? guidString)
    {
        if (string.IsNullOrWhiteSpace(guidString))
            return false;

        return Guid.TryParse(guidString, out _);
    }

    /// <summary>
    /// Limit string length to prevent DoS attacks
    /// </summary>
    public static string LimitLength(string? input, int maxLength = 5000)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return input.Length > maxLength
            ? input.Substring(0, maxLength)
            : input;
    }
}
