using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using TicketMasala.Web.Engine.GERDA.Estimating;
using TicketMasala.Web.Models;
using TicketMasala.Tests.TestHelpers;

namespace TicketMasala.Tests.Services.GERDA;

public class GerdaEstimatingServiceTests
{
    private readonly Mock<ILogger<GerdaEstimatingService>> _mockLogger;

    public GerdaEstimatingServiceTests()
    {
        _mockLogger = new Mock<ILogger<GerdaEstimatingService>>();
    }

    [Fact]
    public async Task EstimateEffortAsync_SimpleTicket_ReturnsLowEffort()
    {
        // Arrange
        var service = new GerdaEstimatingService(_mockLogger.Object);
        var ticket = TestDataBuilder.BuildTicket();
        ticket.Description = "Simple password reset request";

        // Act
        var result = await service.EstimateEffortAsync(ticket);

        // Assert
        result.Should().BeGreaterThan(0).And.BeLessThan(5);
    }

    [Fact]
    public async Task EstimateEffortAsync_ComplexTicket_ReturnsHighEffort()
    {
        // Arrange
        var service = new GerdaEstimatingService(_mockLogger.Object);
        var ticket = TestDataBuilder.BuildTicket();
        ticket.Description = "Complex multi-system integration requiring database migration, API changes, and extensive testing across multiple environments";

        // Act
        var result = await service.EstimateEffortAsync(ticket);

        // Assert
        result.Should().BeGreaterThan(8);
    }

    [Fact]
    public async Task EstimateEffortAsync_WithUrgentKeywords_IncreasesEffort()
    {
        // Arrange
        var service = new GerdaEstimatingService(_mockLogger.Object);
        var ticket1 = TestDataBuilder.BuildTicket();
        ticket1.Description = "Update user profile";
        
        var ticket2 = TestDataBuilder.BuildTicket();
        ticket2.Description = "URGENT CRITICAL: Update user profile immediately";

        // Act
        var effort1 = await service.EstimateEffortAsync(ticket1);
        var effort2 = await service.EstimateEffortAsync(ticket2);

        // Assert
        effort2.Should().BeGreaterThanOrEqualTo(effort1);
    }

    [Fact]
    public async Task EstimateEffortAsync_NullDescription_ThrowsException()
    {
        // Arrange
        var service = new GerdaEstimatingService(_mockLogger.Object);
        var ticket = TestDataBuilder.BuildTicket();
        ticket.Description = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.EstimateEffortAsync(ticket));
    }

    [Fact]
    public async Task EstimateEffortAsync_EmptyDescription_ReturnsMinimumEffort()
    {
        // Arrange
        var service = new GerdaEstimatingService(_mockLogger.Object);
        var ticket = TestDataBuilder.BuildTicket();
        ticket.Description = "";

        // Act
        var result = await service.EstimateEffortAsync(ticket);

        // Assert
        result.Should().Be(1); // Minimum effort
    }

    [Theory]
    [InlineData("bug fix", 3)]
    [InlineData("new feature development", 8)]
    [InlineData("security patch", 5)]
    [InlineData("documentation update", 2)]
    public async Task EstimateEffortAsync_WithKeywordPatterns_ReturnsExpectedRange(string description, int expectedEffort)
    {
        // Arrange
        var service = new GerdaEstimatingService(_mockLogger.Object);
        var ticket = TestDataBuilder.BuildTicket();
        ticket.Description = description;

        // Act
        var result = await service.EstimateEffortAsync(ticket);

        // Assert
        result.Should().BeCloseTo(expectedEffort, 3); // Within 3 points
    }
}
