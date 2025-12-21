using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TicketMasala.Web.Tenancy;

namespace TicketMasala.Web.Tests.UnitTests.Tenancy;

public class PluginAdapterTests
{
    [Fact]
    public void Constructor_WithNullTenantPlugin_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new PluginAdapter(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("tenantPlugin");
    }

    [Fact]
    public void PluginId_ReturnsFormattedId()
    {
        // Arrange
        var mockPlugin = new Mock<ITenantPlugin>();
        mockPlugin.Setup(p => p.TenantId).Returns("TestTenant");
        var adapter = new PluginAdapter(mockPlugin.Object);

        // Act
        var result = adapter.PluginId;

        // Assert
        result.Should().Be("TicketMasala.TestTenant");
    }

    [Fact]
    public void DisplayName_DelegatesToTenantPlugin()
    {
        // Arrange
        var mockPlugin = new Mock<ITenantPlugin>();
        mockPlugin.Setup(p => p.DisplayName).Returns("Test Display Name");
        var adapter = new PluginAdapter(mockPlugin.Object);

        // Act
        var result = adapter.DisplayName;

        // Assert
        result.Should().Be("Test Display Name");
    }

    [Fact]
    public void ConfigureServices_DelegatesToTenantPlugin()
    {
        // Arrange
        var mockPlugin = new Mock<ITenantPlugin>();
        var adapter = new PluginAdapter(mockPlugin.Object);
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        adapter.ConfigureServices(services, configuration);

        // Assert
        mockPlugin.Verify(p => p.ConfigureServices(services, configuration), Times.Once);
    }

    [Fact]
    public void Extension_ToStandardPlugin_ReturnsAdapter()
    {
        // Arrange
        var mockPlugin = new Mock<ITenantPlugin>();
        mockPlugin.Setup(p => p.TenantId).Returns("TestTenant");

        // Act
        var result = mockPlugin.Object.ToStandardPlugin();

        // Assert
        result.Should().BeOfType<PluginAdapter>();
        result.PluginId.Should().Be("TicketMasala.TestTenant");
    }
}
