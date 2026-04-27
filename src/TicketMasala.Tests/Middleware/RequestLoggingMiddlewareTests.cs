using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using TicketMasala.Web.Middleware;
using Xunit;

namespace TicketMasala.Tests.Middleware;

public class RequestLoggingMiddlewareTests
{
    [Fact(DisplayName = "InvokeAsync logs successful request completion")]
    public async Task InvokeAsync_Logs_Successful_Request()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/test-path";
        context.Response.StatusCode = 200;

        var loggerMock = new Mock<ILogger<RequestLoggingMiddleware>>();
        var middleware = new RequestLoggingMiddleware(
            next: (innerHttpContext) => Task.CompletedTask,
            logger: loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact(DisplayName = "InvokeAsync logs warning for 4xx errors")]
    public async Task InvokeAsync_Logs_Warning_For_4xx_Errors()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/test";
        context.Response.StatusCode = 400;

        var loggerMock = new Mock<ILogger<RequestLoggingMiddleware>>();
        var middleware = new RequestLoggingMiddleware(
            next: (innerHttpContext) => Task.CompletedTask,
            logger: loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request completed with error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact(DisplayName = "InvokeAsync does not log warning for 404 errors")]
    public async Task InvokeAsync_Does_Not_Log_Warning_For_404()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/not-found";
        context.Response.StatusCode = 404;

        var loggerMock = new Mock<ILogger<RequestLoggingMiddleware>>();
        var middleware = new RequestLoggingMiddleware(
            next: (innerHttpContext) => Task.CompletedTask,
            logger: loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Should log as Information, not Warning
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request completed with error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact(DisplayName = "InvokeAsync logs warning for slow requests")]
    public async Task InvokeAsync_Logs_Warning_For_Slow_Requests()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/slow-endpoint";
        context.Response.StatusCode = 200;

        var loggerMock = new Mock<ILogger<RequestLoggingMiddleware>>();
        var middleware = new RequestLoggingMiddleware(
            next: async (innerHttpContext) =>
            {
                // Simulate slow request (> 500ms)
                await Task.Delay(600);
            },
            logger: loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Slow request detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact(DisplayName = "InvokeAsync logs error for exceptions")]
    public async Task InvokeAsync_Logs_Error_For_Exceptions()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/error-endpoint";

        var loggerMock = new Mock<ILogger<RequestLoggingMiddleware>>();
        var middleware = new RequestLoggingMiddleware(
            next: (innerHttpContext) => throw new InvalidOperationException("Test exception"),
            logger: loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request failed with exception")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact(DisplayName = "InvokeAsync logs warning for cancelled requests")]
    public async Task InvokeAsync_Logs_Warning_For_Cancelled_Requests()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/cancelled-endpoint";

        var loggerMock = new Mock<ILogger<RequestLoggingMiddleware>>();
        var middleware = new RequestLoggingMiddleware(
            next: (innerHttpContext) => throw new OperationCanceledException(),
            logger: loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.InvokeAsync(context));

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request cancelled")),
                It.IsAny<OperationCanceledException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact(DisplayName = "InvokeAsync logs debug at request start")]
    public async Task InvokeAsync_Logs_Debug_At_Request_Start()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/data";
        context.Response.StatusCode = 200;

        var loggerMock = new Mock<ILogger<RequestLoggingMiddleware>>();
        var middleware = new RequestLoggingMiddleware(
            next: (innerHttpContext) => Task.CompletedTask,
            logger: loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request started")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
