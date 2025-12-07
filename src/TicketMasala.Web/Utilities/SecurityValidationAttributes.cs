using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Web.Utilities;
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

    /// <summary>
    /// Validate file upload size and type
    /// </summary>
    public class SafeFileUploadAttribute : ValidationAttribute
    {
        private readonly long _maxFileSize;
        private readonly string[] _allowedExtensions;

        public SafeFileUploadAttribute(long maxFileSizeInMB, params string[] allowedExtensions)
        {
            _maxFileSize = maxFileSizeInMB * 1024 * 1024; // Convert MB to bytes
            _allowedExtensions = allowedExtensions.Select(e => e.ToLowerInvariant()).ToArray();
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not IFormFile file)
                return ValidationResult.Success;

            // Check file size
            if (file.Length > _maxFileSize)
            {
                return new ValidationResult($"File size exceeds maximum allowed size of {_maxFileSize / 1024 / 1024} MB");
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                return new ValidationResult($"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}");
            }

            return ValidationResult.Success;
        }
}
