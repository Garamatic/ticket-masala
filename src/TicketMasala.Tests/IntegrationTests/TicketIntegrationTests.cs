using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TicketMasala.Tests.IntegrationTests;

public class TicketIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TicketIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetTickets_Unauthenticated_RedirectsToLogin()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/Ticket");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("/Identity/Account/Login", location);
        Assert.Contains("ReturnUrl=%2FTicket", location);
    }

    [Fact]
    public async Task GetComponents_Unauthenticated_RedirectsToLogin()
    {
        // Many components might be protected
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        // Assuming /Ticket/Create is a valid route
        var response = await client.GetAsync("/Ticket/Create");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("/Identity/Account/Login", location);
    }
}
