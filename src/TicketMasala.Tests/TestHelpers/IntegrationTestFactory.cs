using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Domain.Data;
using TicketMasala.Web.Engine.GERDA.Estimating;
using Moq;

namespace TicketMasala.Tests.TestHelpers;

public class IntegrationTestFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly string _tempConfigPath;

    public IntegrationTestFactory()
    {
        _tempConfigPath = Path.Combine(Path.GetTempPath(), "ticket_masala_test_config_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempConfigPath);

        // Create dummy configuration files required for startup
        File.WriteAllText(Path.Combine(_tempConfigPath, "masala_domains.yaml"), "domains: {}\nglobal:\n  default_domain: IT");
        File.WriteAllText(Path.Combine(_tempConfigPath, "gerda_config.yaml"), "gerda:\n  is_enabled: false");

        Environment.SetEnvironmentVariable("MASALA_CONFIG_PATH", _tempConfigPath);
        TicketMasala.Web.Configuration.ConfigurationPaths.ResetCache();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<MasalaDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Add InMemory DbContext
            services.AddDbContext<MasalaDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
                options.EnableSensitiveDataLogging();
            });

            // Mock EstimatingService to avoid complex Gerda setup
            services.AddScoped<IEstimatingService>(sp => Mock.Of<IEstimatingService>());
        });

        builder.UseEnvironment("Development");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

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
