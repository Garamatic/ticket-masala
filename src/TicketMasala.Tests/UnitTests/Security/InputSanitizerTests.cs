using TicketMasala.Domain.Common;
using Xunit;

namespace TicketMasala.Tests.UnitTests.Security;

public class InputSanitizerTests
{
    [Fact]
    public void SanitizeHtml_ShouldRemoveScripts()
    {
        // Arrange
        var input = "Hello <script>alert('xss')</script>World";

        // Act
        var result = InputSanitizer.SanitizeHtml(input);

        // Assert
        Assert.DoesNotContain("<script>", result);
        Assert.DoesNotContain("alert('xss')", result);
        Assert.Contains("Hello", result);
        Assert.Contains("World", result);
    }

    [Fact]
    public void SanitizeHtml_ShouldKeepSafeTags()
    {
        // Arrange
        var input = "<b>Bold</b> and <i>Italic</i>";

        // Act
        var result = InputSanitizer.SanitizeHtml(input);

        // Assert
        Assert.Contains("<b>Bold</b>", result);
        Assert.Contains("<i>Italic</i>", result);
    }

    [Fact]
    public void SanitizeHtml_ShouldRemoveEventHandlers()
    {
        // Arrange
        var input = "<a href='#' onclick='alert(1)'>Click me</a>";

        // Act
        var result = InputSanitizer.SanitizeHtml(input);

        // Assert
        Assert.DoesNotContain("onclick", result);
        Assert.Contains("Click me", result);
    }

    [Fact]
    public void SanitizeHtml_ShouldHandleNull()
    {
        // Act
        var result = InputSanitizer.SanitizeHtml(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }
}
