using TicketMasala.Web;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TicketMasala.Tests.IntegrationTests;

public class LocalizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LocalizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("fr", "Bienvenue sur Ticket Masala")]
    [InlineData("nl", "Welkom bij Ticket Masala")]
    public async Task Homepage_WithQueryString_ReturnsLocalizedContent(string culture, string expectedText)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/?culture={culture}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Debug: Check if culture was actually switched
        Assert.Contains($"<html lang=\"{culture}\">", content);

        // Check translation
        Assert.Contains(expectedText, content);
    }

    [Theory]
    [InlineData("fr", "Bienvenue sur Ticket Masala")]
    [InlineData("nl", "Welkom bij Ticket Masala")]
    public async Task Homepage_WithAcceptLanguageHeader_ReturnsLocalizedContent(string culture, string expectedText)
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", culture);

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains($"<html lang=\"{culture}\">", content);
        Assert.Contains(expectedText, content);
    }

    [Fact]
    public async Task SetCulture_Controller_RedirectsAndSetsCookie()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/Culture/SetCulture?culture=fr&returnUrl=/");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode); // LocalRedirect is 302 or similar

        // Check for Set-Cookie header
        Assert.True(response.Headers.Contains("Set-Cookie"));
        var cookie = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
        Assert.NotNull(cookie);
        Assert.Contains(".AspNetCore.Culture", cookie);
        Assert.Contains("c%3Dfr%7Cuic%3Dfr", cookie); // c=fr|uic=fr URL encoded
    }
}
