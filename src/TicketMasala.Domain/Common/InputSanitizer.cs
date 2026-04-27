using System.Text.RegularExpressions;
using System.Web;
using Ganss.Xss;

namespace TicketMasala.Domain.Common;

/// <summary>
/// Utility class for sanitizing user input to prevent XSS and injection attacks
/// </summary>
public static class InputSanitizer
{
    /// <summary>
    /// Remove potentially dangerous HTML tags and scripts using HtmlSanitizer library (Don't reinvent the wheel).
    /// </summary>
    public static string SanitizeHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var sanitizer = new HtmlSanitizer();
        return sanitizer.Sanitize(input);
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
    /// Validate and sanitize email addresses
    /// </summary>
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            // Simple regex for email validation
            var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, emailPattern);
        }
        catch
        {
            return false;
        }
    }
}
