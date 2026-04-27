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
        var errors = ValidateCore(options, _logger).ToList();
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

    /// <summary>
    /// Validates configuration at startup and throws if invalid.
    /// Static version for use during early startup when DI is not available.
    /// </summary>
    public static void ValidateOrThrow(MasalaOptions options, ILogger logger)
    {
        var errors = ValidateCore(options, logger).ToList();

        if (errors.Count > 0)
        {
            logger.LogError("Configuration validation failed with {ErrorCount} errors", errors.Count);
            foreach (var error in errors)
            {
                logger.LogError("  - {Error}", error);
            }
            throw new ConfigurationValidationException(errors.ToArray());
        }

        logger.LogInformation("Configuration validation successful");
    }

    /// <summary>
    /// Core validation logic shared between instance and static validation methods.
    /// </summary>
    private static IEnumerable<ValidationError> ValidateCore(MasalaOptions options, ILogger? logger)
    {
        var errors = new List<ValidationError>();

        // Validate using data annotations
        var context = new System.ComponentModel.DataAnnotations.ValidationContext(options);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            foreach (var result in results)
            {
                var field = result.MemberNames.FirstOrDefault() ?? "Unknown";
                errors.Add(new ValidationError(field, result.ErrorMessage ?? "Validation failed"));
            }
        }

        // Validate nested objects
        errors.AddRange(ValidateDatabaseOptionsCore(options.Database));
        errors.AddRange(ValidateGerdaOptionsCore(options.Gerda, logger));
        errors.AddRange(ValidateFeatureFlagsCore(options.Features));

        return errors;
    }

    private static IEnumerable<ValidationError> ValidateDatabaseOptionsCore(DatabaseOptions options)
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

    private static IEnumerable<ValidationError> ValidateFeatureFlagsCore(FeatureOptions options)
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

    private static IEnumerable<ValidationError> ValidateGerdaOptionsCore(GerdaOptions options, ILogger? logger)
    {
        var errors = new List<ValidationError>();

        if (options.Enabled && string.IsNullOrWhiteSpace(options.ModelPath))
        {
            // ModelPath is optional, just log a warning
            logger?.LogWarning("GERDA is enabled but ModelPath is not configured");
        }

        return errors;
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

    // Validation methods now use the shared core validation logic in ValidateCore

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
