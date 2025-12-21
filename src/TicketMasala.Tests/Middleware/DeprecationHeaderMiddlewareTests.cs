using Microsoft.AspNetCore.Http;
using TicketMasala.Web.Middleware;
using Xunit;

namespace TicketMasala.Tests.Middleware;

public class DeprecationHeaderMiddlewareTests
{
    [Theory]
    [InlineData("/api/tickets", "/api/v1/work-items")]
    [InlineData("/api/tickets/123", "/api/v1/work-items")]
    [InlineData("/api/projects", "/api/v1/work-containers")]
    [InlineData("/api/projects/abc", "/api/v1/work-containers")]
    public async Task InvokeAsync_Should_Add_Headers_For_Deprecated_Routes(string path, string expectedLinkUrl)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        var middleware = new DeprecationHeaderMiddleware(next: (innerHttpContext) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Deprecation"));
        Assert.Equal("true", context.Response.Headers["Deprecation"]);

        Assert.True(context.Response.Headers.ContainsKey("Link"));
        var linkHeader = context.Response.Headers["Link"].ToString();
        Assert.Contains($"<{expectedLinkUrl}>; rel=\"alternate\"", linkHeader);
        
        Assert.True(context.Response.Headers.ContainsKey("X-API-Deprecation-Warning"));
    }

    [Theory]
    [InlineData("/api/v1/work-items")]
    [InlineData("/health")]
    [InlineData("/")]
    public async Task InvokeAsync_Should_Not_Add_Headers_For_Normal_Routes(string path)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        var middleware = new DeprecationHeaderMiddleware(next: (innerHttpContext) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("Deprecation"));
        Assert.False(context.Response.Headers.ContainsKey("Link"));
    }
}
