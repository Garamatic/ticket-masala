using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using TicketMasala.Web.Tenancy;

namespace TicketMasala.Web.Tests.IntegrationTests;

public class PluginIntegrationTests
{
    // A simple service interface for testing
    public interface ITestService
    {
        string GetMessage();
    }

    // A simple service implementation
    public class TestService : ITestService
    {
        public string GetMessage() => "Hello from Test Plugin";
    }

    // A concrete implementation of ITenantPlugin for testing
    public class TestTenantPlugin : ITenantPlugin
    {
        public string TenantId => "integration-test-tenant";
        public string DisplayName => "Integration Test Tenant";

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register a service to verify it gets populated
            services.AddSingleton<ITestService, TestService>();
        }

        public void ConfigureMiddleware(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // No middleware needed for this test
        }
    }

    [Fact]
    public void PluginAdapter_ShouldRegisterServices_InRealContainer()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        
        var tenantPlugin = new TestTenantPlugin();
        var standardPlugin = new PluginAdapter(tenantPlugin);

        // Act
        // Simulate a host loading the plugin via the Standard interface
        standardPlugin.ConfigureServices(services, configuration);
        
        var serviceProvider = services.BuildServiceProvider();
        var testService = serviceProvider.GetService<ITestService>();

        // Assert
        testService.Should().NotBeNull();
        testService!.GetMessage().Should().Be("Hello from Test Plugin");
    }

    [Fact]
    public void PluginAdapter_ShouldExposeCorrectMetadata()
    {
        // Arrange
        var tenantPlugin = new TestTenantPlugin();
        var standardPlugin = new PluginAdapter(tenantPlugin);

        // Act & Assert
        standardPlugin.PluginId.Should().Be("TicketMasala.integration-test-tenant");
        standardPlugin.DisplayName.Should().Be("Integration Test Tenant");
    }
}
