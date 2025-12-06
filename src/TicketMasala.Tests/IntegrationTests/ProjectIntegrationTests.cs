using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TicketMasala.Tests.IntegrationTests;

public class ProjectIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProjectIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProjects_Unauthenticated_RedirectsToLogin()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/Projects");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("/Identity/Account/Login", location);
        Assert.Contains("ReturnUrl=%2FProjects", location);
    }
}
