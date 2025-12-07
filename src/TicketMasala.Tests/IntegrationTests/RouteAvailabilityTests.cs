using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using TicketMasala.Web;

namespace TicketMasala.Tests.IntegrationTests;

public class RouteAvailabilityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RouteAvailabilityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/Customer")]
    [InlineData("/Ticket")]
    public async Task ProtectedRoutes_RedirectToLogin_WhenUnauthorized(string url)
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false // We want to check for 302, not follow it
        });

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Identity/Account/Login", response.Headers.Location?.LocalPath);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Identity/Account/Login")]
    [InlineData("/Identity/Account/Register")]
    public async Task PublicRoutes_ReturnSuccess(string url)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(response.IsSuccessStatusCode);
    }
}
