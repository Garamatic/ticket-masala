using Microsoft.Extensions.Options;
using TicketMasala.Web.Configuration;

namespace TicketMasala.Web.Extensions;

/// <summary>
/// Extension methods for configuring strongly-typed configuration options.
/// </summary>
public static class ConfigurationServiceCollectionExtensions
{
    /// <summary>
    /// Adds strongly-typed configuration options and validation to the service collection.
    /// </summary>
    public static IServiceCollection AddMasalaConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind MasalaOptions from configuration
        services.Configure<MasalaOptions>(configuration.GetSection(MasalaOptions.SectionName));

        // Register the configuration validator
        services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();

        // Add options validation
        services.AddOptions<MasalaOptions>()
            .Bind(configuration.GetSection(MasalaOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Bind OpenAiSettings
        services.AddOptions<OpenAiSettings>()
            .Bind(configuration.GetSection(OpenAiSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }


    /// <summary>
    /// Validates configuration at startup and throws if invalid.
    /// </summary>
    public static IServiceCollection ValidateMasalaConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger logger)
    {
        var options = new MasalaOptions();
        configuration.GetSection(MasalaOptions.SectionName).Bind(options);

        // Create a temporary validator for startup validation
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var validatorLogger = loggerFactory.CreateLogger<ConfigurationValidator>();
        var validator = new ConfigurationValidator(validatorLogger);

        try
        {
            validator.ValidateOrThrow(options);
            logger.LogInformation("Masala configuration validated successfully");
        }
        catch (ConfigurationValidationException ex)
        {
            logger.LogCritical(ex, "Configuration validation failed. Application cannot start.");
            throw;
        }

        return services;
    }
}
