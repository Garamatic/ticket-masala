using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicketMasala.Domain.Data;
using TicketMasala.Web.Extensions;

namespace TicketMasala.Tests.TestHelpers;

/// <summary>
/// Test factory with GERDA configured for testing.
/// Uses in-memory database and mock ML predictions.
/// </summary>
public class IntegrationTestFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly string _tempConfigPath;
    private readonly string? _originalEnvVar;

    public IntegrationTestFactory()
    {
        _tempConfigPath = Path.Combine(Path.GetTempPath(), "ticket_masala_test_config_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempConfigPath);

        // Capture original environment variable
        _originalEnvVar = Environment.GetEnvironmentVariable("MASALA_CONFIG_PATH");

        // Create test configuration with GERDA disabled
        var testConfig = new Dictionary<string, object>
        {
            ["AppInstanceName"] = "Test Ticket Masala",
            ["GerdaAI"] = new Dictionary<string, object>
            {
                ["IsEnabled"] = false
            }
        };

        var configJson = System.Text.Json.JsonSerializer.Serialize(testConfig);
        File.WriteAllText(Path.Combine(_tempConfigPath, "masala_config.json"), configJson);

        Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", _tempConfigPath);
        TicketMasala.Web.Configuration.ConfigurationPaths.ResetCache();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            var testSettings = new Dictionary<string, string?>
            {
                ["DatabaseProvider"] = "InMemory"
            };

            configBuilder.AddInMemoryCollection(testSettings);
        });

        builder.ConfigureServices((context, services) =>
        {
            // Re-register GERDA with test options
            var gerdaOptions = new GerdaOptions
            {
                ConfigBasePath = _tempConfigPath,
                UseMockMlPredictions = true,
                EnableConfigReload = false
            };

            // Remove existing GERDA registrations if any
            // Use precise namespace matching to avoid removing unrelated services
            var descriptorsToRemove = services
                .Where(d => d.ServiceType.FullName?.StartsWith("TicketMasala.Web.Engine.GERDA") == true)
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Re-add with test configuration
            services.AddGerda(options =>
            {
                options.ConfigBasePath = gerdaOptions.ConfigBasePath;
                options.UseMockMlPredictions = gerdaOptions.UseMockMlPredictions;
                options.EnableConfigReload = gerdaOptions.EnableConfigReload;
            });
        });

        builder.UseEnvironment("Testing");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // Restore original environment variable and reset cache
        Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", _originalEnvVar);
        TicketMasala.Web.Configuration.ConfigurationPaths.ResetCache();

        if (Directory.Exists(_tempConfigPath))
        {
            try
            {
                Directory.Delete(_tempConfigPath, true);
            }
            catch { /* Ignore cleanup errors */ }
        }
    }
}
