using Moq;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Engine.GERDA.Estimating;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Tests.TestHelpers;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Tests.UnitTests.Ingestion;

public class EmailTicketProcessorTests
{
    private readonly DatabaseTestFixture _fixture;
    private readonly Mock<IEstimatingService> _mockEstimatingService;
    private readonly Mock<ILogger<EmailTicketProcessor>> _mockLogger;

    public EmailTicketProcessorTests()
    {
        _fixture = new DatabaseTestFixture();
        _mockEstimatingService = new Mock<IEstimatingService>();
        _mockLogger = new Mock<ILogger<EmailTicketProcessor>>();
    }

    [Fact]
    public async Task ProcessEmailAsync_CreatesTicketWithSentiment()
    {
        // Arrange
        var processor = new EmailTicketProcessor(_fixture.Context, _mockEstimatingService.Object, _mockLogger.Object);
        var email = new EmailContent("URGENT: Database Down", "The production database is unresponsive.", "user@test.com");

        // Act
        var ticket = await processor.ProcessEmailAsync(email, CancellationToken.None);

        // Assert
        Assert.NotNull(ticket);
        Assert.NotEqual(Guid.Empty, ticket.Guid);
        Assert.Equal("URGENT: Database Down", ticket.Title);
        Assert.True(ticket.PriorityScore >= 4.0, "Expected strict urgency score");
        Assert.Contains("Sentiment-Critical", ticket.GerdaTags);

        // Verify Estimating was called
        _mockEstimatingService.Verify(x => x.EstimateComplexityAsync(ticket.Guid), Times.Once);

        // Verify Saved to DB
        var savedTicket = await _fixture.Context.Tickets.FindAsync(ticket.Guid);
        Assert.NotNull(savedTicket);
    }

    [Fact]
    public async Task ProcessEmailAsync_HandlesNeutralEmail()
    {
        // Arrange
        var processor = new EmailTicketProcessor(_fixture.Context, _mockEstimatingService.Object, _mockLogger.Object);
        var email = new EmailContent("Question about features", "Can you tell me more?", "user@test.com");

        // Act
        var ticket = await processor.ProcessEmailAsync(email, CancellationToken.None);

        // Assert
        Assert.Equal(1.0, ticket.PriorityScore);
        Assert.Contains("Sentiment-Normal", ticket.GerdaTags);
    }
}
