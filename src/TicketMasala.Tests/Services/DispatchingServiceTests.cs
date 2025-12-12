using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Engine.GERDA.Dispatching;
using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Engine.GERDA.Strategies;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Web.Models;
using TicketMasala.Web;
using TicketMasala.Web.Data;

namespace TicketMasala.Tests.Services;

public class DispatchingServiceTests
{
    private readonly Mock<ILogger<DispatchingService>> _mockLogger;
    private readonly DbContextOptions<MasalaDbContext> _dbOptions;
    private readonly GerdaConfig _config;

    public DispatchingServiceTests()
    {
        _mockLogger = new Mock<ILogger<DispatchingService>>();

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
                    MinHistoryForAffinityMatch = 10 // High enough to force fallback
                }
            }
        };
    }

    [Fact]
    public async Task GetRecommendedAgentAsync_FreshSystem_ReturnsAgentViaFallback()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var strategyFactory = new Mock<IStrategyFactory>();
        var domainConfig = new Mock<IDomainConfigurationService>();
        // Mock fallback strategy
        var mockStrategy = new Mock<IDispatchingStrategy>();
        mockStrategy.Setup(x => x.GetRecommendedAgentsAsync(It.IsAny<Ticket>(), It.IsAny<int>()))
            .ReturnsAsync(new List<(string, double)> { ("emp1", 1.0) });

        strategyFactory.Setup(x => x.GetStrategy<IDispatchingStrategy, List<(string, double)>>(It.IsAny<string>()))
            .Returns(mockStrategy.Object);

        var service = new DispatchingService(context, _config, strategyFactory.Object, domainConfig.Object, _mockLogger.Object);

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
            Level = EmployeeType.Support
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
            Level = EmployeeType.Support
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
            TicketStatus = Status.Pending
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
    public async Task GetRecommendedAgentAsync_WithWorkload_ReturnsLeastBusyAgent()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var strategyFactory = new Mock<IStrategyFactory>();
        var domainConfig = new Mock<IDomainConfigurationService>();
        // Mock fallback strategy
        var mockStrategy = new Mock<IDispatchingStrategy>();
        mockStrategy.Setup(x => x.GetRecommendedAgentsAsync(It.IsAny<Ticket>(), It.IsAny<int>()))
            .ReturnsAsync(new List<(string, double)> { ("free", 1.0) });

        strategyFactory.Setup(x => x.GetStrategy<IDispatchingStrategy, List<(string, double)>>(It.IsAny<string>()))
            .Returns(mockStrategy.Object);

        var service = new DispatchingService(context, _config, strategyFactory.Object, domainConfig.Object, _mockLogger.Object);

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
            Level = EmployeeType.Support
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
            Level = EmployeeType.Support
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
                TicketStatus = Status.Assigned
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
            TicketStatus = Status.Pending
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetRecommendedAgentAsync(ticket.Guid);

        // Assert
        Assert.Equal(freeEmployee.Id, result);
    }
}
