using Moq;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Engine.GERDA.Estimating;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Tests.TestHelpers;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Tests.UnitTests.Ingestion;

public class EmailTicketProcessorTests
{
    private readonly DatabaseTestFixture _fixture;
    private readonly Mock<ITicketWorkflowService> _mockWorkflowService;
    private readonly Mock<IEstimatingService> _mockEstimatingService;
    private readonly Mock<ILogger<EmailTicketProcessor>> _mockLogger;

    public EmailTicketProcessorTests()
    {
        _fixture = new DatabaseTestFixture();
        _mockWorkflowService = new Mock<ITicketWorkflowService>();
        _mockEstimatingService = new Mock<IEstimatingService>();
        _mockLogger = new Mock<ILogger<EmailTicketProcessor>>();
    }

    [Fact]
    public async Task ProcessEmailAsync_CreatesTicketWithSentiment()
    {
        // Arrange
        var mockTicket = new Ticket { Guid = Guid.NewGuid(), Title = "URGENT: Database Down", GerdaTags = "" };
        _mockWorkflowService.Setup(s => s.CreateTicketAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(mockTicket);

        var processor = new EmailTicketProcessor(_mockWorkflowService.Object, _mockEstimatingService.Object, _mockLogger.Object);
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

        // Verify Service Calls (since it's a mock, we don't check DB here)
        _mockWorkflowService.Verify(s => s.CreateTicketAsync(It.IsAny<string>(), "system-email", null, null, It.IsAny<DateTime?>()), Times.Once);
        _mockWorkflowService.Verify(s => s.UpdateTicketAsync(It.Is<Ticket>(t => t.Guid == ticket.Guid && t.PriorityScore >= 4.0)), Times.Once);
    }

    [Fact]
    public async Task ProcessEmailAsync_HandlesNeutralEmail()
    {
        // Arrange
        var mockTicket = new Ticket { Guid = Guid.NewGuid(), Title = "Question about features", GerdaTags = "" };
        _mockWorkflowService.Setup(s => s.CreateTicketAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(mockTicket);

        var processor = new EmailTicketProcessor(_mockWorkflowService.Object, _mockEstimatingService.Object, _mockLogger.Object);
        var email = new EmailContent("Question about features", "Can you tell me more?", "user@test.com");

        // Act
        var ticket = await processor.ProcessEmailAsync(email, CancellationToken.None);

        // Assert
        Assert.Equal(1.0, ticket.PriorityScore);
        Assert.Contains("Sentiment-Normal", ticket.GerdaTags);
        _mockWorkflowService.Verify(s => s.UpdateTicketAsync(It.IsAny<Ticket>()), Times.Once);
    }
}
