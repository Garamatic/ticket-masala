using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Engine.Ingestion.Background;
using TicketMasala.Web.Models;
using TicketMasala.Web.Data;
using Customer = TicketMasala.Web.Models.ApplicationUser;

namespace TicketMasala.Tests.Services;

public class MetricsServiceTests
{
    private readonly Mock<ILogger<MetricsService>> _mockLogger;
    private readonly DbContextOptions<MasalaDbContext> _dbOptions;

    public MetricsServiceTests()
    {
        _mockLogger = new Mock<ILogger<MetricsService>>();

        // Use in-memory database for testing
        _dbOptions = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task CalculateTeamMetricsAsync_WithNoTickets_ReturnsEmptyMetrics()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = new MetricsService(context, _mockLogger.Object);

        // Act
        var result = await service.CalculateTeamMetricsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalActiveTickets);
        Assert.Equal(0, result.UnassignedTickets);
        Assert.Equal(0, result.CompletedTickets);
    }

    [Fact]
    public async Task CalculateTeamMetricsAsync_WithTickets_ReturnsCorrectCounts()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);

        var customer = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "test@example.com",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "Customer",
            Phone = "123456789"
        };

        var employee = new Employee
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "employee@example.com",
            Email = "employee@example.com",
            FirstName = "Test",
            LastName = "Employee",
            Phone = "987654321",
            Team = "Support",
            Level = EmployeeType.Support
        };

        context.Users.AddRange(customer, employee);

        context.Tickets.AddRange(
            new Ticket
            {
                Description = "Pending ticket",
                Customer = customer,
                TicketStatus = Status.Pending
            },
            new Ticket
            {
                Description = "Assigned ticket",
                Customer = customer,
                Responsible = employee,
                TicketStatus = Status.Assigned
            },
            new Ticket
            {
                Description = "Completed ticket",
                Customer = customer,
                Responsible = employee,
                TicketStatus = Status.Completed
            }
        );

        await context.SaveChangesAsync();

        var service = new MetricsService(context, _mockLogger.Object);

        // Act
        var result = await service.CalculateTeamMetricsAsync();

        // Assert
        Assert.Equal(2, result.TotalActiveTickets); // Pending + Assigned
        Assert.Equal(1, result.UnassignedTickets);   // Pending
        Assert.Equal(1, result.AssignedTickets);     // Assigned
        Assert.Equal(1, result.CompletedTickets);    // Completed
    }

    [Fact]
    public async Task CalculateTeamMetricsAsync_WithPriorityScores_ReturnsCorrectAverage()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);

        var customer = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "test@example.com",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "Customer",
            Phone = "123456789"
        };

        context.Users.Add(customer);

        context.Tickets.AddRange(
            new Ticket
            {
                Description = "High priority",
                Customer = customer,
                TicketStatus = Status.Pending,
                PriorityScore = 15.0
            },
            new Ticket
            {
                Description = "Medium priority",
                Customer = customer,
                TicketStatus = Status.Pending,
                PriorityScore = 10.0
            }
        );

        await context.SaveChangesAsync();

        var service = new MetricsService(context, _mockLogger.Object);

        // Act
        var result = await service.CalculateTeamMetricsAsync();

        // Assert
        Assert.Equal(12.5, result.AveragePriorityScore); // (15.0 + 10.0) / 2
    }

    [Fact]
    public async Task CalculateTeamMetricsAsync_WithAgentWorkload_ReturnsCorrectUtilization()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);

        var customer = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "test@example.com",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "Customer",
            Phone = "123456789"
        };

        var employee = new Employee
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "employee@example.com",
            Email = "employee@example.com",
            FirstName = "Agent",
            LastName = "One",
            Phone = "987654321",
            Team = "Support",
            Level = EmployeeType.Support,
            MaxCapacityPoints = 40
        };

        context.Users.AddRange(customer, employee);

        context.Tickets.AddRange(
            new Ticket
            {
                Description = "Ticket 1",
                Customer = customer,
                Responsible = employee,
                TicketStatus = Status.Assigned,
                EstimatedEffortPoints = 8
            },
            new Ticket
            {
                Description = "Ticket 2",
                Customer = customer,
                Responsible = employee,
                TicketStatus = Status.Assigned,
                EstimatedEffortPoints = 13
            }
        );

        await context.SaveChangesAsync();

        var service = new MetricsService(context, _mockLogger.Object);

        // Act
        var result = await service.CalculateTeamMetricsAsync();

        // Assert
        Assert.Single(result.AgentWorkloads);
        var agentMetric = result.AgentWorkloads.First();
        Assert.Equal(21, agentMetric.CurrentWorkload); // 8 + 13
        Assert.Equal(40, agentMetric.MaxCapacity);
        Assert.Equal(52.5, agentMetric.UtilizationPercentage); // (21/40)*100
    }

    [Fact]
    public async Task CalculateTeamMetricsAsync_WithSlaTargets_CalculatesComplianceRate()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);

        var customer = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "test@example.com",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "Customer",
            Phone = "123456789"
        };

        context.Users.Add(customer);

        var tomorrow = DateTime.UtcNow.AddDays(1);
        var yesterday = DateTime.UtcNow.AddDays(-1);

        context.Tickets.AddRange(
            new Ticket
            {
                Description = "Within SLA",
                Customer = customer,
                TicketStatus = Status.Pending,
                CompletionTarget = tomorrow
            },
            new Ticket
            {
                Description = "Breaching SLA",
                Customer = customer,
                TicketStatus = Status.Pending,
                CompletionTarget = yesterday
            }
        );

        await context.SaveChangesAsync();

        var service = new MetricsService(context, _mockLogger.Object);

        // Act
        var result = await service.CalculateTeamMetricsAsync();

        // Assert
        Assert.Equal(1, result.TicketsWithinSla);
        Assert.Equal(1, result.TicketsBreachingSla);
        Assert.Equal(50.0, result.SlaComplianceRate); // (1/2)*100
    }
}
