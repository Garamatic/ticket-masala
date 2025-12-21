using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace TicketMasala.Domain.Common;

/// <summary>
/// Validates that a field is required only if another field has a specific value.
/// Useful for conditional validation in forms.
/// </summary>
/// <example>
/// [RequiredIf("IsNewCustomer", true, ErrorMessage = "First name is required for new customer")]
/// public string? NewCustomerFirstName { get; set; }
/// </example>
public class RequiredIfAttribute : ValidationAttribute
{
    private readonly string _dependentProperty;
    private readonly object _targetValue;

    public RequiredIfAttribute(string dependentProperty, object targetValue)
    {
        _dependentProperty = dependentProperty;
        _targetValue = targetValue;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var property = validationContext.ObjectType.GetProperty(_dependentProperty);
        if (property == null)
        {
            return new ValidationResult($"Unknown property: {_dependentProperty}");
        }

        var dependentValue = property.GetValue(validationContext.ObjectInstance);

        // Check if the dependent property has the target value
        if (Equals(dependentValue, _targetValue))
        {
            // If it does, the current property is required
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} is required.");
            }
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Validates that a string contains only valid name characters (letters, spaces, hyphens, apostrophes)
/// </summary>
public class ValidNameAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success; // Use [Required] for required validation
        }

        var name = value.ToString()!;

        // Allow letters, spaces, hyphens, and apostrophes
        if (!Regex.IsMatch(name, @"^[a-zA-Z\s\-']+$"))
        {
            return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} contains invalid characters. Only letters, spaces, hyphens and apostrophes are allowed.");
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Custom validation attribute to prevent XSS attacks in user input
/// </summary>
public class NoHtmlAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return ValidationResult.Success;

        var input = value.ToString()!;

        // Check for dangerous HTML tags
        var dangerousPatterns = new[]
        {
                "<script",
                "</script",
                "<iframe",
                "javascript:",
                "onerror=",
                "onload=",
                "onclick=",
                "<object",
                "<embed",
                "data:text/html"
            };

        foreach (var pattern in dangerousPatterns)
        {
            if (input.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult($"Input contains potentially dangerous content: {pattern}");
            }
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Validate string length with reasonable limits
/// </summary>
public class SafeStringLengthAttribute : StringLengthAttribute
{
    public SafeStringLengthAttribute(int maximumLength) : base(maximumLength)
    {
        if (maximumLength > 10000)
        {
            throw new ArgumentException("Maximum length should not exceed 10000 characters to prevent DoS attacks");
        }
    }
}

/// <summary>
/// Validate that a string does not contain SQL injection patterns
/// Note: This is a defense-in-depth measure. Always use parameterized queries.
/// </summary>
public class NoSqlInjectionAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return ValidationResult.Success;

        var input = value.ToString()!.ToLowerInvariant();

        // Check for SQL injection patterns
        var sqlPatterns = new[]
        {
                "' or '1'='1",
                "'; drop table",
                "'; delete from",
                "union select",
                "exec(",
                "execute(",
                "sp_executesql",
                "xp_cmdshell"
            };

        foreach (var pattern in sqlPatterns)
        {
            if (input.Contains(pattern))
            {
                return new ValidationResult("Input contains potentially dangerous SQL patterns");
            }
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Validate that JSON input is safe
/// </summary>
public class SafeJsonAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return ValidationResult.Success;

        var input = value.ToString()!;

        try
        {
            // Try to parse as JSON to ensure it's valid
            System.Text.Json.JsonDocument.Parse(input);

            // Check for dangerous patterns in JSON
            if (input.Contains("__proto__") || input.Contains("constructor"))
            {
                return new ValidationResult("JSON contains potentially dangerous prototype pollution patterns");
            }

            return ValidationResult.Success;
        }
        catch (System.Text.Json.JsonException)
        {
            return new ValidationResult("Invalid JSON format");
        }
    }
}
