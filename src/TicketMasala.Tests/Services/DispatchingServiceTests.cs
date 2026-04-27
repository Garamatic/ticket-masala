using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Web;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Web.Engine.GERDA.Dispatching;
using TicketMasala.Web.Engine.GERDA.Dispatching.Algorithms;
using TicketMasala.Web.Engine.GERDA.Dispatching.Configuration;
using TicketMasala.Web.Engine.GERDA.Dispatching.Models;
using TicketMasala.Web.Engine.GERDA.Models;
using Xunit;
using DispatchResultEntity = TicketMasala.Web.Engine.GERDA.Dispatching.DispatchResult;

namespace TicketMasala.Tests.Services;

/// <summary>
/// Tests for the consolidated DispatchingService (Issue #7).
/// 
/// ARCHITECTURE CHANGE:
/// - Old: Strategy-based dispatching with competing paths
/// - New: AgentMatchingEngine + IAffinityScorer plugins
/// 
/// These tests verify backward compatibility and new consolidated behavior.
/// </summary>
public class DispatchingServiceTests
{
    private readonly Mock<ILogger<DispatchingService>> _mockLogger;
    private readonly Mock<ILogger<AgentMatchingEngine>> _mockEngineLogger;
    private readonly Mock<ILogger<IAffinityScorer>> _mockAffinityLogger;
    private readonly DbContextOptions<MasalaDbContext> _dbOptions;
    private readonly GerdaConfig _config;
    private readonly DispatchingConfig _dispatchingConfig;

    public DispatchingServiceTests()
    {
        _mockLogger = new Mock<ILogger<DispatchingService>>();
        _mockEngineLogger = new Mock<ILogger<AgentMatchingEngine>>();
        _mockAffinityLogger = new Mock<ILogger<IAffinityScorer>>();

        _dbOptions = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDispatchDb_" + Guid.NewGuid())
            .Options;

        _config = new GerdaConfig
        {
            GerdaAI = new GerdaAISettings
            {
                IsEnabled = true,
                Dispatching = new DispatchingSettings
                {
                    IsEnabled = true,
                    MaxAssignedTicketsPerAgent = 10,
                    MinHistoryForAffinityMatch = 10
                }
            }
        };

        _dispatchingConfig = new DispatchingConfig
        {
            MaxCasesPerAgent = 10,
            ConfidenceThreshold = 70m,
            OptimalUtilizationThreshold = 0.6m,
            SkillMatchWeight = 0.35m,
            WorkloadBalanceWeight = 0.30m,
            AffinityWeight = 0.25m,
            AvailabilityWeight = 0.10m
        };
    }

    private DispatchingService CreateService(
        MasalaDbContext context,
        IAffinityScorer? affinityScorer = null,
        IDispatchingStrategy? legacyStrategy = null)
    {
        var autoDispatchPolicy = new Mock<IAutoDispatchPolicy>();
        autoDispatchPolicy.Setup(x => x.ShouldAutoDispatch(It.IsAny<DispatchResultEntity>(), out It.Ref<double>.IsAny))
            .Returns((DispatchResultEntity result, out double threshold) =>
            {
                threshold = 0.6;
                return result?.Score >= threshold;
            });

        var pmRecommendationService = new Mock<IProjectManagerRecommendationService>();

        var agentMatchingEngine = new Mock<AgentMatchingEngine>(_dispatchingConfig, _mockEngineLogger.Object, affinityScorer);

        return new DispatchingService(
            context,
            _config,
            autoDispatchPolicy.Object,
            pmRecommendationService.Object,
            agentMatchingEngine.Object,
            affinityScorer ?? Mock.Of<IAffinityScorer>(),
            legacyStrategy,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetRecommendedAgentAsync_WithAvailableEmployees_ReturnsAgent()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);

        var mockAffinityScorer = new Mock<IAffinityScorer>();
        mockAffinityScorer.Setup(x => x.IsReady).Returns(false);

        var service = CreateService(context, mockAffinityScorer.Object);

        var customer = new ApplicationUser
        {
            Id = "customer-id",
            UserName = "customer@example.com",
            Email = "customer@example.com",
            FirstName = "John",
            LastName = "Doe",
            Phone = "123-456-7890"
        };
        var employee1 = new Employee
        {
            Id = "emp1",
            UserName = "emp1",
            Email = "emp1@test.com",
            FirstName = "Emp",
            LastName = "One",
            Phone = "123",
            Team = "Support",
            Level = TicketMasala.Domain.Common.EmployeeType.Support
        };
        var employee2 = new Employee
        {
            Id = "emp2",
            UserName = "emp2",
            Email = "emp2@test.com",
            FirstName = "Emp",
            LastName = "Two",
            Phone = "123",
            Team = "Support",
            Level = TicketMasala.Domain.Common.EmployeeType.Support
        };

        context.Users.AddRange(customer, employee1, employee2);

        var ticket = new Ticket
        {
            Guid = Guid.NewGuid(),
            Description = "Test Ticket",
            DomainId = "IT",
            Status = "New",
            Title = "Test Ticket",
            CustomFieldsJson = "{}",
            TicketStatus = TicketMasala.Domain.Common.Status.Pending
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetRecommendedAgentAsync(ticket.Guid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result, new[] { employee1.Id, employee2.Id });
    }

    [Fact]
    public async Task GetRecommendedAgentAsync_WithWorkload_UsesConsolidatedEngine()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);

        var mockAffinityScorer = new Mock<IAffinityScorer>();
        mockAffinityScorer.Setup(x => x.IsReady).Returns(false);

        // Use the real engine to test the actual consolidated logic
        var autoDispatchPolicy = new Mock<IAutoDispatchPolicy>();
        autoDispatchPolicy.Setup(x => x.ShouldAutoDispatch(It.IsAny<DispatchResultEntity>(), out It.Ref<double>.IsAny))
            .Returns(true);

        var pmRecommendationService = new Mock<IProjectManagerRecommendationService>();

        var agentMatchingEngine = new AgentMatchingEngine(_dispatchingConfig, _mockEngineLogger.Object, mockAffinityScorer.Object);

        var service = new DispatchingService(
            context,
            _config,
            autoDispatchPolicy.Object,
            pmRecommendationService.Object,
            agentMatchingEngine,
            mockAffinityScorer.Object,
            null,
            _mockLogger.Object);

        var customer = new ApplicationUser
        {
            Id = "customer-id",
            UserName = "customer@example.com",
            Email = "customer@example.com",
            FirstName = "John",
            LastName = "Doe",
            Phone = "123-456-7890"
        };
        var busyEmployee = new Employee
        {
            Id = "busy",
            UserName = "busy",
            Email = "busy@test.com",
            FirstName = "Busy",
            LastName = "Bee",
            Phone = "123",
            Team = "Support",
            Level = TicketMasala.Domain.Common.EmployeeType.Support
        };
        var freeEmployee = new Employee
        {
            Id = "free",
            UserName = "free",
            Email = "free@test.com",
            FirstName = "Free",
            LastName = "Bird",
            Phone = "123",
            Team = "Support",
            Level = TicketMasala.Domain.Common.EmployeeType.Support
        };

        context.Users.AddRange(customer, busyEmployee, freeEmployee);

        // Assign 5 tickets to busy employee
        for (int i = 0; i < 5; i++)
        {
            context.Tickets.Add(new Ticket
            {
                Guid = Guid.NewGuid(),
                Description = $"Busy Ticket {i}",
                DomainId = "IT",
                Status = "New",
                Title = $"Busy Ticket {i}",
                CustomFieldsJson = "{}",
                ResponsibleId = busyEmployee.Id,
                TicketStatus = TicketMasala.Domain.Common.Status.Assigned
            });
        }

        var ticket = new Ticket
        {
            Guid = Guid.NewGuid(),
            Description = "New Ticket",
            DomainId = "IT",
            Status = "New",
            Title = "New Ticket",
            CustomFieldsJson = "{}",
            TicketStatus = TicketMasala.Domain.Common.Status.Pending
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetRecommendedAgentAsync(ticket.Guid);

        // Assert - the consolidated engine returns one of the available agents
        Assert.NotNull(result);
        Assert.Contains(result, new[] { busyEmployee.Id, freeEmployee.Id });
    }

    [Fact]
    public void LastModelTrainingTime_ReturnsAffinityScorerValue()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var expectedTime = DateTime.UtcNow.AddHours(-1);

        var mockAffinityScorer = new Mock<IAffinityScorer>();
        mockAffinityScorer.Setup(x => x.LastTrained).Returns(expectedTime);
        mockAffinityScorer.Setup(x => x.IsReady).Returns(true);

        var service = CreateService(context, mockAffinityScorer.Object);

        // Act
        var result = service.LastModelTrainingTime;

        // Assert
        Assert.Equal(expectedTime, result);
    }

    [Fact]
    public void IsEnabled_WhenDispatchingDisabled_ReturnsFalse()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var disabledConfig = new GerdaConfig
        {
            GerdaAI = new GerdaAISettings
            {
                IsEnabled = true,
                Dispatching = new DispatchingSettings { IsEnabled = false }
            }
        };

        var autoDispatchPolicy = new Mock<IAutoDispatchPolicy>();
        var pmRecommendationService = new Mock<IProjectManagerRecommendationService>();
        var mockAffinityScorer = new Mock<IAffinityScorer>();
        var mockEngine = new Mock<AgentMatchingEngine>(_dispatchingConfig, _mockEngineLogger.Object, mockAffinityScorer.Object);

        var service = new DispatchingService(
            context,
            disabledConfig,
            autoDispatchPolicy.Object,
            pmRecommendationService.Object,
            mockEngine.Object,
            mockAffinityScorer.Object,
            null,
            _mockLogger.Object);

        // Act & Assert
        Assert.False(service.IsEnabled);
    }

    [Fact]
    public async Task GetRecommendedAgentAsync_WhenDisabled_ReturnsNull()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var disabledConfig = new GerdaConfig
        {
            GerdaAI = new GerdaAISettings
            {
                IsEnabled = true,
                Dispatching = new DispatchingSettings { IsEnabled = false }
            }
        };

        var autoDispatchPolicy = new Mock<IAutoDispatchPolicy>();
        var pmRecommendationService = new Mock<IProjectManagerRecommendationService>();
        var mockAffinityScorer = new Mock<IAffinityScorer>();
        var mockEngine = new Mock<AgentMatchingEngine>(_dispatchingConfig, _mockEngineLogger.Object, mockAffinityScorer.Object);

        var service = new DispatchingService(
            context,
            disabledConfig,
            autoDispatchPolicy.Object,
            pmRecommendationService.Object,
            mockEngine.Object,
            mockAffinityScorer.Object,
            null,
            _mockLogger.Object);

        // Act
        var result = await service.GetRecommendedAgentAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTopRecommendedAgentsAsync_WithAffinityScorer_UsesAffinityScores()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);

        var mockAffinityScorer = new Mock<IAffinityScorer>();
        mockAffinityScorer.Setup(x => x.IsReady).Returns(true);
        mockAffinityScorer.Setup(x => x.CalculateAffinity(It.IsAny<Employee>(), It.IsAny<Ticket>(), It.IsAny<ApplicationUser?>()))
            .Returns((Employee emp, Ticket t, ApplicationUser? c) => emp.Id == "emp1" ? 4.5 : 2.5);
        mockAffinityScorer.Setup(x => x.GetAffinityExplanation(It.IsAny<double>(), It.IsAny<Employee>(), It.IsAny<Ticket>()))
            .Returns("Test explanation");

        var service = CreateService(context, mockAffinityScorer.Object);

        var customer = new ApplicationUser
        {
            Id = "customer-id",
            UserName = "customer@example.com",
            Email = "customer@example.com",
            FirstName = "John",
            LastName = "Doe"
        };
        var employee1 = new Employee
        {
            Id = "emp1",
            UserName = "emp1",
            Email = "emp1@test.com",
            FirstName = "High",
            LastName = "Affinity",
            Team = "Support"
        };
        var employee2 = new Employee
        {
            Id = "emp2",
            UserName = "emp2",
            Email = "emp2@test.com",
            FirstName = "Low",
            LastName = "Affinity",
            Team = "Support"
        };

        context.Users.AddRange(customer, employee1, employee2);

        var ticket = new Ticket
        {
            Guid = Guid.NewGuid(),
            Description = "Test Ticket",
            DomainId = "IT",
            Status = "New",
            Title = "Test Ticket",
            CustomFieldsJson = "{}",
            TicketStatus = TicketMasala.Domain.Common.Status.Pending
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        // Act
        var results = await service.GetTopRecommendedAgentsAsync(ticket.Guid, count: 2);

        // Assert
        Assert.NotEmpty(results);

        // Verify affinity scorer was called
        mockAffinityScorer.Verify(x => x.CalculateAffinity(It.IsAny<Employee>(), It.IsAny<Ticket>(), It.IsAny<ApplicationUser?>()), Times.AtLeastOnce);
    }
}
