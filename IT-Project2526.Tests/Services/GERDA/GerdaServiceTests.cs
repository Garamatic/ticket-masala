using IT_Project2526.Services.GERDA;
using IT_Project2526.Services.GERDA.Models;
using IT_Project2526.Services.GERDA.Grouping;
using IT_Project2526.Services.GERDA.Estimating;
using IT_Project2526.Services.GERDA.Ranking;
using IT_Project2526.Services.GERDA.Dispatching;
using IT_Project2526.Models;
using IT_Project2526.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace IT_Project2526.Tests.Services.GERDA;

public class GerdaServiceTests
{
    private readonly Mock<ITicketRepository> _ticketRepositoryMock;
    private readonly Mock<IGroupingService> _groupingServiceMock;
    private readonly Mock<IEstimatingService> _estimatingServiceMock;
    private readonly Mock<IRankingService> _rankingServiceMock;
    private readonly Mock<IDispatchingService> _dispatchingServiceMock;
    private readonly Mock<ILogger<GerdaService>> _loggerMock;
    private readonly GerdaConfig _config;

    public GerdaServiceTests()
    {
        _ticketRepositoryMock = new Mock<ITicketRepository>();
        _groupingServiceMock = new Mock<IGroupingService>();
        _estimatingServiceMock = new Mock<IEstimatingService>();
        _rankingServiceMock = new Mock<IRankingService>();
        _dispatchingServiceMock = new Mock<IDispatchingService>();
        _loggerMock = new Mock<ILogger<GerdaService>>();

        _config = new GerdaConfig
        {
            GerdaAI = new GerdaAISettings { IsEnabled = true }
        };
    }

    private GerdaService CreateService()
    {
        return new GerdaService(
            _ticketRepositoryMock.Object,
            _config,
            _loggerMock.Object,
            _groupingServiceMock.Object,
            _estimatingServiceMock.Object,
            _rankingServiceMock.Object,
            _dispatchingServiceMock.Object);
    }

    [Fact]
    public void IsEnabled_WhenConfigEnabled_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        Assert.True(service.IsEnabled);
    }

    [Fact]
    public void IsEnabled_WhenConfigDisabled_ReturnsFalse()
    {
        // Arrange
        _config.GerdaAI.IsEnabled = false;
        var service = CreateService();

        // Act & Assert
        Assert.False(service.IsEnabled);
    }

    [Fact]
    public async Task ProcessTicketAsync_WhenDisabled_DoesNotProcess()
    {
        // Arrange
        _config.GerdaAI.IsEnabled = false;
        var service = CreateService();
        var ticketGuid = Guid.NewGuid();

        // Act
        await service.ProcessTicketAsync(ticketGuid);

        // Assert - no services should be called
        _groupingServiceMock.Verify(x => x.CheckAndGroupTicketAsync(It.IsAny<Guid>()), Times.Never);
        _estimatingServiceMock.Verify(x => x.EstimateComplexityAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ProcessTicketAsync_CallsGroupingService()
    {
        // Arrange
        var service = CreateService();
        var ticketGuid = Guid.NewGuid();
        _groupingServiceMock.Setup(x => x.CheckAndGroupTicketAsync(ticketGuid))
            .ReturnsAsync((Guid?)null);
        _estimatingServiceMock.Setup(x => x.EstimateComplexityAsync(ticketGuid))
            .ReturnsAsync(5);

        // Act
        await service.ProcessTicketAsync(ticketGuid);

        // Assert
        _groupingServiceMock.Verify(x => x.CheckAndGroupTicketAsync(ticketGuid), Times.Once);
    }

    [Fact]
    public async Task ProcessTicketAsync_CallsEstimatingService()
    {
        // Arrange
        var service = CreateService();
        var ticketGuid = Guid.NewGuid();
        _groupingServiceMock.Setup(x => x.CheckAndGroupTicketAsync(ticketGuid))
            .ReturnsAsync((Guid?)null);
        _estimatingServiceMock.Setup(x => x.EstimateComplexityAsync(ticketGuid))
            .ReturnsAsync(10);

        // Act
        await service.ProcessTicketAsync(ticketGuid);

        // Assert
        _estimatingServiceMock.Verify(x => x.EstimateComplexityAsync(ticketGuid), Times.Once);
    }

    [Fact]
    public async Task ProcessTicketAsync_WhenRankingEnabled_CallsRankingService()
    {
        // Arrange
        var service = CreateService();
        var ticketGuid = Guid.NewGuid();
        
        _groupingServiceMock.Setup(x => x.CheckAndGroupTicketAsync(ticketGuid))
            .ReturnsAsync((Guid?)null);
        _estimatingServiceMock.Setup(x => x.EstimateComplexityAsync(ticketGuid))
            .ReturnsAsync(5);
        _rankingServiceMock.Setup(x => x.IsEnabled).Returns(true);
        _rankingServiceMock.Setup(x => x.CalculatePriorityScoreAsync(ticketGuid))
            .ReturnsAsync(75);

        // Act
        await service.ProcessTicketAsync(ticketGuid);

        // Assert
        _rankingServiceMock.Verify(x => x.CalculatePriorityScoreAsync(ticketGuid), Times.Once);
    }

    [Fact]
    public async Task ProcessTicketAsync_WhenDispatchingEnabled_CallsDispatchingService()
    {
        // Arrange
        var service = CreateService();
        var ticketGuid = Guid.NewGuid();
        
        _groupingServiceMock.Setup(x => x.CheckAndGroupTicketAsync(ticketGuid))
            .ReturnsAsync((Guid?)null);
        _estimatingServiceMock.Setup(x => x.EstimateComplexityAsync(ticketGuid))
            .ReturnsAsync(5);
        _rankingServiceMock.Setup(x => x.IsEnabled).Returns(false);
        _dispatchingServiceMock.Setup(x => x.IsEnabled).Returns(true);
        _dispatchingServiceMock.Setup(x => x.GetRecommendedAgentAsync(ticketGuid))
            .ReturnsAsync("agent-123");

        // Act
        await service.ProcessTicketAsync(ticketGuid);

        // Assert
        _dispatchingServiceMock.Verify(x => x.GetRecommendedAgentAsync(ticketGuid), Times.Once);
    }

    [Fact]
    public async Task ProcessAllOpenTicketsAsync_WhenDisabled_DoesNotProcess()
    {
        // Arrange
        _config.GerdaAI.IsEnabled = false;
        var service = CreateService();

        // Act
        await service.ProcessAllOpenTicketsAsync();

        // Assert
        _ticketRepositoryMock.Verify(x => x.GetAllAsync(It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAllOpenTicketsAsync_ProcessesOpenTickets()
    {
        // Arrange
        var service = CreateService();
        var customer = new Customer { FirstName = "Test", LastName = "Customer", Phone = "555-1234" };
        var tickets = new List<Ticket>
        {
            new() { Guid = Guid.NewGuid(), Description = "Test 1", Customer = customer, TicketStatus = Status.Pending },
            new() { Guid = Guid.NewGuid(), Description = "Test 2", Customer = customer, TicketStatus = Status.Assigned },
            new() { Guid = Guid.NewGuid(), Description = "Test 3", Customer = customer, TicketStatus = Status.Completed } // Should be excluded
        };
        
        _ticketRepositoryMock.Setup(x => x.GetAllAsync(null))
            .ReturnsAsync(tickets);
        _groupingServiceMock.Setup(x => x.CheckAndGroupTicketAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid?)null);
        _estimatingServiceMock.Setup(x => x.EstimateComplexityAsync(It.IsAny<Guid>()))
            .ReturnsAsync(5);

        // Act
        await service.ProcessAllOpenTicketsAsync();

        // Assert - only 2 open tickets should be processed
        _groupingServiceMock.Verify(x => x.CheckAndGroupTicketAsync(It.IsAny<Guid>()), Times.Exactly(2));
    }
}
