using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.GERDA.Dispatching;
using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Repositories;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace TicketMasala.Tests.Services.GERDA;

public class DispatchBacklogServiceTests
{
    private readonly Mock<ITicketRepository> _mockTicketRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IProjectRepository> _mockProjectRepo;
    private readonly Mock<IDispatchingService> _mockDispatchService;
    private readonly Mock<ILogger<DispatchBacklogService>> _mockLogger;

    public DispatchBacklogServiceTests()
    {
        _mockTicketRepo = new Mock<ITicketRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockProjectRepo = new Mock<IProjectRepository>();
        _mockDispatchService = new Mock<IDispatchingService>();
        _mockLogger = new Mock<ILogger<DispatchBacklogService>>();
    }

    [Fact]
    public async Task BuildDispatchBacklogViewModelAsync_PopulatesExplainabilityReasons()
    {
        // Arrange
        var service = new DispatchBacklogService(
            _mockTicketRepo.Object, 
            _mockUserRepo.Object, 
            _mockProjectRepo.Object, 
            _mockDispatchService.Object, 
            _mockLogger.Object);

        var ticket = new Ticket 
        { 
            Guid = Guid.NewGuid(), 
            Title = "Test Ticket", 
            TicketStatus = Status.Pending,
            CreationDate = DateTime.UtcNow
        };
        _mockTicketRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Ticket> { ticket });
        _mockTicketRepo.Setup(r => r.GetByResponsibleIdAsync(It.IsAny<string>())).ReturnsAsync(new List<Ticket>());

        var agent = new Employee 
        { 
            Id = "agent1", 
            FirstName = "Agent", 
            LastName = "One",
            Team = "Support"
        };
        _mockUserRepo.Setup(r => r.GetAllEmployeesAsync()).ReturnsAsync(new List<Employee> { agent });

        _mockProjectRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Project>());

        _mockDispatchService.Setup(d => d.IsEnabled).Returns(true);
        var reasons = new List<string> { "High affinity", "Low workload" };
        var dispatchResult = new DispatchResult(agent.Id, 0.9) { Reasons = reasons };
        _mockDispatchService.Setup(d => d.GetTopRecommendedAgentsAsync(ticket.Guid, 3))
            .ReturnsAsync(new List<DispatchResult> { dispatchResult });

        // Act
        var result = await service.BuildDispatchBacklogViewModelAsync();

        // Assert
        Assert.Single(result.UnassignedTickets);
        var ticketInfo = result.UnassignedTickets.First();
        Assert.Single(ticketInfo.RecommendedAgents);
        var recommendation = ticketInfo.RecommendedAgents.First();
        Assert.Equal(reasons, recommendation.Reasons);
        Assert.Equal("High affinity", recommendation.Reasons[0]);
        Assert.Equal("Low workload", recommendation.Reasons[1]);
    }
}
