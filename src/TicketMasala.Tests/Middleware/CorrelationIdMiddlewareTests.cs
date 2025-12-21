using Microsoft.AspNetCore.Http;
using TicketMasala.Web.Middleware;
using Xunit;

namespace TicketMasala.Tests.Middleware;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Should_Generate_New_CorrelationId_If_Missing()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(next: (innerHttpContext) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var itemCorrelationId = context.Items[CorrelationIdMiddleware.ContextKey];
        var headerCorrelationId = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();

        Assert.NotNull(itemCorrelationId);
        Assert.NotNull(headerCorrelationId);
        Assert.NotEmpty(itemCorrelationId.ToString());
        Assert.Equal(itemCorrelationId.ToString(), headerCorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_Should_Use_Existing_CorrelationId_From_Header()
    {
        // Arrange
        var expectedCorrelationId = "existing-correlation-id";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = expectedCorrelationId;

        var middleware = new CorrelationIdMiddleware(next: (innerHttpContext) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var itemCorrelationId = context.Items[CorrelationIdMiddleware.ContextKey];
        var headerCorrelationId = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();

        Assert.Equal(expectedCorrelationId, itemCorrelationId);
        Assert.Equal(expectedCorrelationId, headerCorrelationId);
    }
}
