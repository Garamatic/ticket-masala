using Microsoft.Extensions.Logging;
using Moq;
using TicketMasala.Web.Engine.Security;
using Xunit;

namespace TicketMasala.Tests.UnitTests.Security;

public class PiiScrubberTests
{
    private readonly PiiScrubberService _scrubber;

    public PiiScrubberTests()
    {
        var loggerMock = new Mock<ILogger<PiiScrubberService>>();
        _scrubber = new PiiScrubberService(loggerMock.Object);
    }

    [Fact]
    public void Scrub_ShouldRemoveEmails()
    {
        // Arrange
        var input = "Contact me at john.doe@example.com for details.";

        // Act
        var result = _scrubber.Scrub(input);

        // Assert
        Assert.Contains("[EMAIL_REDACTED]", result);
        Assert.DoesNotContain("john.doe@example.com", result);
    }

    [Fact]
    public void Scrub_ShouldRemoveBelgianNISS()
    {
        // Arrange
        var input = "My national number is 85.01.01-123.45 thanks.";

        // Act
        var result = _scrubber.Scrub(input);

        // Assert
        Assert.Contains("[NISS_REDACTED]", result);
        Assert.DoesNotContain("85.01.01-123.45", result);
    }

    [Fact]
    public void Scrub_ShouldRemovePhoneNumbers()
    {
        // Arrange
        var input1 = "Call +32 475 12 34 56 now";
        var input2 = "Or 0475 12 34 56 local";

        // Act
        var result1 = _scrubber.Scrub(input1);
        var result2 = _scrubber.Scrub(input2);

        // Assert
        Assert.Contains("[PHONE_REDACTED]", result1);
        Assert.DoesNotContain("+32 475 12 34 56", result1);
        
        Assert.Contains("[PHONE_REDACTED]", result2);
        Assert.DoesNotContain("0475 12 34 56", result2);
    }

    [Fact]
    public void Scrub_ShouldLeaveSafeTextAlone()
    {
        // Arrange
        var input = "This is a safe string with 123 numbers but not PII.";

        // Act
        var result = _scrubber.Scrub(input);

        // Assert
        Assert.Equal(input, result);
    }
}
