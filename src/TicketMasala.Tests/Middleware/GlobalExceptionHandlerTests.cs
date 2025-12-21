using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TicketMasala.Web.Middleware;
using TicketMasala.Web.ViewModels.Api;
using Xunit;
using Moq;

namespace TicketMasala.Tests.Middleware;

public class GlobalExceptionHandlerTests
{
    [Theory]
    [MemberData(nameof(ExceptionTestData))]
    public async Task TryHandleAsync_Should_Return_Consistent_Error_Response(Exception exception, string expectedError, int expectedStatusCode)
    {
        // Arrange
        var logger = Mock.Of<ILogger<GlobalExceptionHandler>>();
        var handler = new GlobalExceptionHandler(logger);
        var correlationId = Guid.NewGuid().ToString();
        var context = CreateHttpContext(correlationId);

        // Act
        var result = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedStatusCode, context.Response.StatusCode);

        var responseBody = GetResponseBody(context);
        var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(errorResponse);
        Assert.Equal(expectedError, errorResponse.Error);
        Assert.Equal(correlationId, errorResponse.CorrelationId);

        if (expectedError == "INTERNAL_ERROR")
        {
            Assert.Equal("An unexpected error occurred. Please try again later.", errorResponse.Message);
        }
        else if (expectedError == "UNAUTHORIZED")
        {
            Assert.Equal("Authentication is required to access this resource.", errorResponse.Message);
        }
        else
        {
            Assert.Equal(exception.Message, errorResponse.Message);
        }
    }

    public static IEnumerable<object[]> ExceptionTestData =>
        new List<object[]>
        {
            new object[] { new ArgumentException("Validation failed"), "VALIDATION_ERROR", 400 },
            new object[] { new KeyNotFoundException("Item not found"), "NOT_FOUND", 404 },
            new object[] { new UnauthorizedAccessException("Access denied"), "UNAUTHORIZED", 401 },
            new object[] { new InvalidOperationException("Invalid state"), "INVALID_OPERATION", 400 },
            new object[] { new Exception("Crash"), "INTERNAL_ERROR", 500 }
        };

    private HttpContext CreateHttpContext(string? correlationId)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        if (correlationId != null)
        {
            context.Items[CorrelationIdMiddleware.ContextKey] = correlationId;
        }

        return context;
    }

    private string GetResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return reader.ReadToEnd();
    }
}
