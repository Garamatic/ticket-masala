using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TicketMasala.Web.Controllers;
using TicketMasala.Web.Engine.GERDA.Anticipation;
using TicketMasala.Web.Engine.GERDA.Dispatching;
using TicketMasala.Web.Engine.GERDA.Ranking;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Models;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.ViewModels.Dashboard;
using TicketMasala.Web.ViewModels.GERDA;
using Xunit;

namespace TicketMasala.Tests.Controllers;

public class ManagerControllerTests
{
    private readonly Mock<ILogger<ManagerController>> _mockLogger;
    private readonly Mock<IMetricsService> _mockMetricsService;
    private readonly Mock<IDispatchingService> _mockDispatchingService;
    private readonly Mock<IRankingService> _mockRankingService;
    private readonly Mock<IDispatchBacklogService> _mockDispatchBacklogService;
    private readonly Mock<ITicketService> _mockTicketService;
    private readonly Mock<IProjectRepository> _mockProjectRepository;
    private readonly Mock<IAnticipationService> _mockAnticipationService;
    private readonly Mock<ITicketGenerator> _mockTicketGenerator;

    private readonly ManagerController _controller;

    public ManagerControllerTests()
    {
        _mockLogger = new Mock<ILogger<ManagerController>>();
        _mockMetricsService = new Mock<IMetricsService>();
        _mockDispatchingService = new Mock<IDispatchingService>();
        _mockRankingService = new Mock<IRankingService>();
        _mockDispatchBacklogService = new Mock<IDispatchBacklogService>();
        _mockTicketService = new Mock<ITicketService>();
        _mockProjectRepository = new Mock<IProjectRepository>();
        _mockAnticipationService = new Mock<IAnticipationService>();
        _mockTicketGenerator = new Mock<ITicketGenerator>();

        _controller = new ManagerController(
            _mockLogger.Object,
            _mockMetricsService.Object,
            _mockTicketService.Object,
            _mockProjectRepository.Object,
            _mockTicketGenerator.Object,
            _mockDispatchingService.Object,
            _mockRankingService.Object,
            _mockDispatchBacklogService.Object,
            _mockAnticipationService.Object
        );
    }

    [Fact]
    public async Task TeamDashboard_ReturnsViewWithMetrics()
    {
        // Arrange
        var expectedVm = new TeamDashboardViewModel();
        _mockMetricsService.Setup(s => s.CalculateTeamMetricsAsync())
            .ReturnsAsync(expectedVm);
        
        // Corrected Types based on IMetricsService definition
        _mockMetricsService.Setup(s => s.CalculateForecastAsync())
            .ReturnsAsync(new List<ForecastData>()); 
            
        _mockMetricsService.Setup(s => s.CalculateClosedTicketsPerAgentAsync())
            .ReturnsAsync(new List<AgentPerformanceMetric>());

        // Act
        var result = await _controller.TeamDashboard();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TeamDashboardViewModel>(viewResult.Model);
        Assert.Same(expectedVm, model);
    }

    [Fact]
    public async Task BatchAssignTickets_UsesGerdaRecommendations_WhenRequested()
    {
        // Arrange
        var ticketGuid = Guid.NewGuid();
        var request = new BatchAssignRequest
        {
            TicketGuids = new List<Guid> { ticketGuid },
            UseGerdaRecommendations = true
        };

        var expectedResult = new BatchAssignResult
        {
            SuccessCount = 1
        };

        _mockDispatchingService.Setup(s => s.IsEnabled).Returns(true);
        _mockDispatchingService.Setup(s => s.GetRecommendedAgentAsync(ticketGuid))
            .ReturnsAsync("agent-1");

        _mockTicketService.Setup(s => s.BatchAssignTicketsAsync(request, It.IsAny<Func<Guid, Task<string?>>>()))
            .Callback<BatchAssignRequest, Func<Guid, Task<string?>>>(async (req, callback) =>
            {
                // Verify callback logic calls dispatching service
                var agent = await callback(ticketGuid);
                Assert.Equal("agent-1", agent);
            })
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.BatchAssignTickets(request);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var data = jsonResult.Value;
        Assert.NotNull(data);
        var successProperty = data.GetType().GetProperty("success");
        var countProperty = data.GetType().GetProperty("successCount");

        Assert.NotNull(successProperty);
        Assert.NotNull(countProperty);
        var successValue = successProperty.GetValue(data);
        var countValue = countProperty.GetValue(data);
        
        Assert.NotNull(successValue);
        Assert.NotNull(countValue);
        Assert.True((bool)successValue);
        Assert.Equal(1, (int)countValue);
    }
}
