using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Repositories;
using TicketMasala.Tests.TestHelpers;
using TicketMasala.Web.Engine.GERDA.Estimating;
using TicketMasala.Web.Engine.GERDA.Sentiment;
using TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Services;
using Xunit;

namespace TicketMasala.Tests.UnitTests.Ingestion;

public class EmailTicketProcessorTests
{
    private readonly DatabaseTestFixture _fixture;
    private readonly Mock<ITicketLifecycle> _mockTicketLifecycle;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ITicketRepository> _mockTicketRepository;
    private readonly Mock<IEstimatingService> _mockEstimatingService;
    private readonly Mock<ISentimentAnalyzer> _mockSentimentAnalyzer;
    private readonly Mock<ILogger<EmailTicketProcessor>> _mockLogger;

    public EmailTicketProcessorTests()
    {
        _fixture = new DatabaseTestFixture();
        _mockTicketLifecycle = new Mock<ITicketLifecycle>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockTicketRepository = new Mock<ITicketRepository>();
        _mockEstimatingService = new Mock<IEstimatingService>();
        _mockSentimentAnalyzer = new Mock<ISentimentAnalyzer>();
        _mockLogger = new Mock<ILogger<EmailTicketProcessor>>();
    }

    [Fact]
    public async Task ProcessEmailAsync_CreatesTicketWithSentiment()
    {
        // Arrange
        var mockTicket = new Ticket { Guid = Guid.NewGuid(), Title = "URGENT: Database Down", GerdaTags = "" };
        _mockTicketLifecycle.Setup(x => x.ExecuteAsync(
                It.IsAny<CreateTicketCommand>(),
                It.IsAny<TicketContext>()))
            .ReturnsAsync(new TicketResult { Success = true, Ticket = mockTicket });

        _mockSentimentAnalyzer.Setup(a => a.Analyze(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((4.0, "Critical"));

        var processor = new EmailTicketProcessor(
            _mockTicketLifecycle.Object,
            _mockUnitOfWork.Object,
            _mockTicketRepository.Object,
            _mockEstimatingService.Object,
            _mockSentimentAnalyzer.Object,
            new SystemClock(),
            _mockLogger.Object);

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

        // Verify Lifecycle Create was called
        _mockTicketLifecycle.Verify(x => x.ExecuteAsync(
            It.Is<CreateTicketCommand>(c => c.Description == email.Body && c.CustomerId == "system-email"),
            It.Is<TicketContext>(ctx => ctx.UserId == "system")), Times.Once);

        // Verify direct repository update and commit for post-creation field changes
        _mockTicketRepository.Verify(x => x.UpdateAsync(It.Is<Ticket>(t => t.Guid == ticket.Guid)), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessEmailAsync_HandlesNeutralEmail()
    {
        // Arrange
        var mockTicket = new Ticket { Guid = Guid.NewGuid(), Title = "Question about features", GerdaTags = "" };
        _mockTicketLifecycle.Setup(x => x.ExecuteAsync(
                It.IsAny<CreateTicketCommand>(),
                It.IsAny<TicketContext>()))
            .ReturnsAsync(new TicketResult { Success = true, Ticket = mockTicket });

        _mockSentimentAnalyzer.Setup(a => a.Analyze(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((1.0, "Normal"));

        var processor = new EmailTicketProcessor(
            _mockTicketLifecycle.Object,
            _mockUnitOfWork.Object,
            _mockTicketRepository.Object,
            _mockEstimatingService.Object,
            _mockSentimentAnalyzer.Object,
            new SystemClock(),
            _mockLogger.Object);

        var email = new EmailContent("Question about features", "Can you tell me more?", "user@test.com");

        // Act
        var ticket = await processor.ProcessEmailAsync(email, CancellationToken.None);

        // Assert
        Assert.Equal(1.0, ticket.PriorityScore);
        Assert.Contains("Sentiment-Normal", ticket.GerdaTags);

        _mockTicketRepository.Verify(x => x.UpdateAsync(It.IsAny<Ticket>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
