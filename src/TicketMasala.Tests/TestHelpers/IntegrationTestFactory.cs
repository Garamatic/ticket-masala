using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TicketMasala.Domain.Data;
using TicketMasala.Web.Engine.GERDA.Estimating;
using Moq;

namespace TicketMasala.Tests.TestHelpers;

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

        // Create dummy configuration files required for startup
        File.WriteAllText(Path.Combine(_tempConfigPath, "masala_domains.yaml"), "domains: {}\nglobal:\n  default_domain: IT");
        File.WriteAllText(Path.Combine(_tempConfigPath, "gerda_config.yaml"), "gerda:\n  is_enabled: false");

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

        builder.ConfigureServices(services =>
        {
            services.AddScoped<IEstimatingService>(sp => Mock.Of<IEstimatingService>());
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
