using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using TicketMasala.Web.Middleware;
using Xunit;

namespace TicketMasala.Tests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    private static IConfiguration GetEmptyConfiguration()
    {
        return new ConfigurationBuilder().Build();
    }

    private static SecurityHeadersMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new SecurityHeadersMiddleware(next, GetEmptyConfiguration());
    }

    [Fact(DisplayName = "InvokeAsync adds Content-Security-Policy header")]
    public async Task InvokeAsync_Adds_Content_Security_Policy_Header()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(next: (innerHttpContext) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("script-src", csp);
        Assert.Contains("style-src", csp);
    }

    [Fact(DisplayName = "InvokeAsync adds X-Content-Type-Options header")]
    public async Task InvokeAsync_Adds_X_Content_Type_Options_Header()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(next: (innerHttpContext) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("X-Content-Type-Options"));
        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
    }

    [Fact(DisplayName = "InvokeAsync adds X-Frame-Options header")]
    public async Task InvokeAsync_Adds_X_Frame_Options_Header()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(next: (innerHttpContext) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("X-Frame-Options"));
        Assert.Equal("SAMEORIGIN", context.Response.Headers["X-Frame-Options"].ToString());
    }

    [Fact(DisplayName = "InvokeAsync adds X-XSS-Protection header")]
    public async Task InvokeAsync_Adds_X_XSS_Protection_Header()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(next: (innerHttpContext) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("X-XSS-Protection"));
        Assert.Equal("1; mode=block", context.Response.Headers["X-XSS-Protection"].ToString());
    }

    [Fact(DisplayName = "InvokeAsync adds Referrer-Policy header")]
    public async Task InvokeAsync_Adds_Referrer_Policy_Header()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(next: (innerHttpContext) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Referrer-Policy"));
        Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"].ToString());
    }

    [Fact(DisplayName = "InvokeAsync adds Permissions-Policy header")]
    public async Task InvokeAsync_Adds_Permissions_Policy_Header()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(next: (innerHttpContext) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Permissions-Policy"));
        var permissions = context.Response.Headers["Permissions-Policy"].ToString();
        Assert.Contains("camera=()", permissions);
        Assert.Contains("microphone=()", permissions);
    }

    [Fact(DisplayName = "InvokeAsync calls next middleware")]
    public async Task InvokeAsync_Calls_Next_Middleware()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        var middleware = CreateMiddleware(next: (innerHttpContext) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact(DisplayName = "InvokeAsync includes Agentic URL in connect-src when configured")]
    public async Task InvokeAsync_Includes_Agentic_Url_In_Connect_Src()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Agentic:ApiUrl", "https://agent.example.com/" }
            })
            .Build();
        var middleware = new SecurityHeadersMiddleware(next: (innerHttpContext) => Task.CompletedTask, config);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        Assert.Contains("connect-src 'self' https://agent.example.com", csp);
    }
}
