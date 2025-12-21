using System.ComponentModel.DataAnnotations;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace TicketMasala.Web.Configuration;

/// <summary>
/// Validates configuration at application startup.
/// </summary>
public class ConfigurationValidator : IConfigurationValidator
{
    private readonly ILogger<ConfigurationValidator> _logger;

    public ConfigurationValidator(ILogger<ConfigurationValidator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public ValidationResult Validate(MasalaOptions options)
    {
        var errors = new List<ValidationError>();

        // Validate using data annotations
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(options);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        if (!Validator.TryValidateObject(options, validationContext, validationResults, validateAllProperties: true))
        {
            foreach (var result in validationResults)
            {
                var field = result.MemberNames.FirstOrDefault() ?? "Unknown";
                errors.Add(new ValidationError(field, result.ErrorMessage ?? "Validation failed"));
            }
        }

        // Validate nested objects
        errors.AddRange(ValidateDatabaseOptions(options.Database));
        errors.AddRange(ValidateGerdaOptions(options.Gerda));
        errors.AddRange(ValidateFeatureFlags(options.Features));

        return errors.Count > 0 ? ValidationResult.Failure(errors.ToArray()) : ValidationResult.Success();
    }


    /// <inheritdoc />
    public void ValidateOrThrow(MasalaOptions options)
    {
        var result = Validate(options);
        if (!result.IsValid)
        {
            _logger.LogError("Configuration validation failed with {ErrorCount} errors", result.Errors.Count);
            foreach (var error in result.Errors)
            {
                _logger.LogError("  - {Error}", error);
            }
            throw new ConfigurationValidationException(result.Errors);
        }

        _logger.LogInformation("Configuration validation successful");
    }

    /// <inheritdoc />
    public ValidationResult ValidateConfigFile(string filePath)
    {
        var errors = new List<ValidationError>();

        // Check file existence
        if (!File.Exists(filePath))
        {
            errors.Add(new ValidationError("ConfigFile", $"Configuration file not found: {filePath}"));
            return ValidationResult.Failure(errors.ToArray());
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        try
        {
            var content = File.ReadAllText(filePath);

            if (extension == ".yaml" || extension == ".yml")
            {
                errors.AddRange(ValidateYamlSyntax(content, filePath));
            }
            else if (extension == ".json")
            {
                errors.AddRange(ValidateJsonSyntax(content, filePath));
            }
        }
        catch (IOException ex)
        {
            errors.Add(new ValidationError("ConfigFile", $"Failed to read configuration file: {ex.Message}"));
        }

        return errors.Count > 0 ? ValidationResult.Failure(errors.ToArray()) : ValidationResult.Success();
    }

    private IEnumerable<ValidationError> ValidateDatabaseOptions(DatabaseOptions options)
    {
        var errors = new List<ValidationError>();
        var context = new System.ComponentModel.DataAnnotations.ValidationContext(options);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            foreach (var result in results)
            {
                var field = $"Database.{result.MemberNames.FirstOrDefault() ?? "Unknown"}";
                errors.Add(new ValidationError(field, result.ErrorMessage ?? "Validation failed"));
            }
        }

        // Additional validation
        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            errors.Add(new ValidationError("Database.Provider", "Database provider is required"));
        }

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            errors.Add(new ValidationError("Database.ConnectionString", "Connection string is required"));
        }

        return errors;
    }


    private IEnumerable<ValidationError> ValidateGerdaOptions(GerdaOptions options)
    {
        var errors = new List<ValidationError>();

        if (options.Enabled && string.IsNullOrWhiteSpace(options.ModelPath))
        {
            // ModelPath is optional, just log a warning
            _logger.LogWarning("GERDA is enabled but ModelPath is not configured");
        }

        return errors;
    }

    private IEnumerable<ValidationError> ValidateFeatureFlags(FeatureFlags options)
    {
        var errors = new List<ValidationError>();
        var context = new System.ComponentModel.DataAnnotations.ValidationContext(options);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            foreach (var result in results)
            {
                var field = $"Features.{result.MemberNames.FirstOrDefault() ?? "Unknown"}";
                errors.Add(new ValidationError(field, result.ErrorMessage ?? "Validation failed"));
            }
        }

        return errors;
    }

    private IEnumerable<ValidationError> ValidateYamlSyntax(string content, string filePath)
    {
        var errors = new List<ValidationError>();

        try
        {
            var deserializer = new DeserializerBuilder().Build();
            deserializer.Deserialize<object>(content);
        }
        catch (YamlException ex)
        {
            var lineNumber = ex.Start.Line;
            errors.Add(new ValidationError(
                "YamlSyntax",
                $"Invalid YAML syntax in {Path.GetFileName(filePath)}: {ex.Message}",
                lineNumber: (int?)lineNumber));
        }

        return errors;
    }

    private IEnumerable<ValidationError> ValidateJsonSyntax(string content, string filePath)
    {
        var errors = new List<ValidationError>();

        try
        {
            System.Text.Json.JsonDocument.Parse(content);
        }
        catch (System.Text.Json.JsonException ex)
        {
            errors.Add(new ValidationError(
                "JsonSyntax",
                $"Invalid JSON syntax in {Path.GetFileName(filePath)}: {ex.Message}",
                (int?)ex.LineNumber));
        }

        return errors;
    }
}
