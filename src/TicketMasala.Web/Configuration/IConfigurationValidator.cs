namespace TicketMasala.Web.Configuration;

/// <summary>
/// Interface for validating configuration at application startup.
/// </summary>
public interface IConfigurationValidator
{
    /// <summary>
    /// Validates the provided configuration options.
    /// </summary>
    /// <param name="options">The configuration options to validate.</param>
    /// <returns>A validation result containing any errors found.</returns>
    ValidationResult Validate(MasalaOptions options);

    /// <summary>
    /// Validates the provided configuration options and throws if invalid.
    /// </summary>
    /// <param name="options">The configuration options to validate.</param>
    /// <exception cref="ConfigurationValidationException">Thrown when validation fails.</exception>
    void ValidateOrThrow(MasalaOptions options);

    /// <summary>
    /// Validates that a configuration file exists and has valid syntax.
    /// </summary>
    /// <param name="filePath">Path to the configuration file.</param>
    /// <returns>A validation result containing any errors found.</returns>
    ValidationResult ValidateConfigFile(string filePath);
}


/// <summary>
/// Result of configuration validation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether the configuration is valid.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// List of validation errors found.
    /// </summary>
    public List<ValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new();

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    public static ValidationResult Failure(params ValidationError[] errors) => new() { Errors = errors.ToList() };
}

/// <summary>
/// Represents a single validation error.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// The field or property that failed validation.
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// The error message describing the validation failure.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The line number in the configuration file where the error occurred (if applicable).
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// Creates a new validation error.
    /// </summary>
    public ValidationError() { }

    /// <summary>
    /// Creates a new validation error with the specified field and message.
    /// </summary>
    public ValidationError(string field, string message, int? lineNumber = null)
    {
        Field = field;
        Message = message;
        LineNumber = lineNumber;
    }

    public override string ToString()
    {
        var location = LineNumber.HasValue ? $" (line {LineNumber})" : "";
        return $"{Field}: {Message}{location}";
    }
}

/// <summary>
/// Exception thrown when configuration validation fails.
/// </summary>
public class ConfigurationValidationException : Exception
{
    /// <summary>
    /// The validation errors that caused the exception.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    public ConfigurationValidationException(IEnumerable<ValidationError> errors)
        : base(FormatMessage(errors))
    {
        Errors = errors.ToList().AsReadOnly();
    }

    public ConfigurationValidationException(string message)
        : base(message)
    {
        Errors = new List<ValidationError> { new("Configuration", message) }.AsReadOnly();
    }

    private static string FormatMessage(IEnumerable<ValidationError> errors)
    {
        var errorList = errors.ToList();
        if (errorList.Count == 1)
            return $"Configuration validation failed: {errorList[0]}";

        return $"Configuration validation failed with {errorList.Count} errors:\n" +
               string.Join("\n", errorList.Select(e => $"  - {e}"));
    }
}
