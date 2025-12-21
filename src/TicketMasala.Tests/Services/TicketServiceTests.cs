using TicketMasala.Web.Engine.GERDA.Tickets.Domain;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Engine.Ingestion.Background;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Observers;
using Microsoft.AspNetCore.Http;
using TicketMasala.Web;
using System.Security.Claims;
using TicketMasala.Web.Engine.Compiler;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Domain.Entities.Configuration;

namespace TicketMasala.Tests.Services;

public class TicketServiceTests
{
    private readonly Mock<ILogger<TicketService>> _mockLogger;
    private readonly DbContextOptions<MasalaDbContext> _dbOptions;

    public TicketServiceTests()
    {
        _mockLogger = new Mock<ILogger<TicketService>>();

        // Use in-memory database for testing
        _dbOptions = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseInMemoryDatabase(databaseName: "TestTicketDb_" + Guid.NewGuid())
            .Options;
    }

    private TicketService CreateService(MasalaDbContext context)
    {
        var ticketRepository = new Mock<ITicketRepository>();
        var userRepository = new Mock<IUserRepository>();
        var projectRepository = new Mock<IProjectRepository>();
        var observers = new List<ITicketObserver>();
        var commentObservers = new List<ICommentObserver>();
        var notificationService = new Mock<INotificationService>();
        var auditService = new Mock<IAuditService>();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var ruleEngine = new Mock<IRuleEngineService>();
        var domainConfig = new Mock<IDomainConfigurationService>();
        var logger = new Mock<ILogger<TicketService>>();

        // Wire up Repository Mocks to use the InMemory Context
        userRepository.Setup(r => r.GetCustomerByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => context.Users.OfType<ApplicationUser>().FirstOrDefault(u => u.Id == id));

        userRepository.Setup(r => r.GetEmployeeByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => context.Users.OfType<Employee>().FirstOrDefault(u => u.Id == id));

        userRepository.Setup(r => r.GetAllCustomersAsync())
            .ReturnsAsync(() => context.Users.OfType<ApplicationUser>().ToList());

        userRepository.Setup(r => r.GetAllEmployeesAsync())
            .ReturnsAsync(() => context.Users.OfType<Employee>().ToList());

        userRepository.Setup(r => r.GetUserByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => context.Users.Find(id));

        // Wire up TicketRepository Mocks
        ticketRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()))
            .ReturnsAsync((Guid id, bool includeRelations) =>
            {
                if (includeRelations)
                {
                    var query = context.Tickets.Include(t => t.Customer).AsQueryable();
                    return query.FirstOrDefault(t => t.Guid == id);
                }
                return context.Tickets.Find(id);
            });

        ticketRepository.Setup(r => r.UpdateAsync(It.IsAny<Ticket>()))
            .Callback((Ticket t) =>
            {
                var existing = context.Tickets.Find(t.Guid);
                if (existing != null)
                {
                    context.Entry(existing).CurrentValues.SetValues(t);
                }
            })
            .Returns(Task.CompletedTask);

        ticketRepository.Setup(r => r.AddAsync(It.IsAny<Ticket>()))
           .Callback((Ticket t) =>
           {
               context.Tickets.Add(t);
           })
           .ReturnsAsync((Ticket t) => t);

        // Add domain services mocks
        var dispatchLogger = new Mock<ILogger<TicketDispatchService>>();
        var reportingLogger = new Mock<ILogger<TicketReportingService>>();
        var notificationLogger = new Mock<ILogger<TicketNotificationService>>();

        var ticketDispatchService = new TicketMasala.Web.Engine.GERDA.Tickets.Domain.TicketDispatchService(ticketRepository.Object, dispatchLogger.Object);
        var ticketReportingService = new TicketMasala.Web.Engine.GERDA.Tickets.Domain.TicketReportingService(ticketRepository.Object, reportingLogger.Object);
        var ticketNotificationService = new TicketMasala.Web.Engine.GERDA.Tickets.Domain.TicketNotificationService(notificationService.Object, notificationLogger.Object);

        return new TicketService(
            context,
            ticketRepository.Object,
            userRepository.Object,
            projectRepository.Object,
            observers,
            commentObservers,
            notificationService.Object,
            auditService.Object,
            httpContextAccessor.Object,
            ruleEngine.Object,
            domainConfig.Object,
            logger.Object,
            ticketDispatchService,
            ticketReportingService,
            ticketNotificationService);
    }

    [Fact]
    public async Task CreateTicketAsync_WithValidData_CreatesTicket()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        var customer = new ApplicationUser
        {
            Id = "11111111-1111-1111-1111-111111111111",
            UserName = "customer@example.com",
            Email = "customer@example.com",
            FirstName = "John",
            LastName = "Doe",
            Phone = "123-456-7890"
        };

        context.Users.Add(customer);
        await context.SaveChangesAsync();

        // Act
        var ticket = await service.CreateTicketAsync(
            description: "Test ticket",
            customerId: customer.Id,
            responsibleId: null,
            projectGuid: null,
            completionTarget: DateTime.UtcNow.AddDays(7)
        );

        // Assert
        Assert.NotNull(ticket);
        Assert.Equal("Test ticket", ticket.Description);
        Assert.Equal(TicketMasala.Domain.Common.Status.Pending, ticket.TicketStatus);
        Assert.Null(ticket.ResponsibleId);
    }

    [Fact]
    public async Task CreateTicketAsync_WithResponsible_SetsStatusToAssigned()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        var customer = new ApplicationUser
        {
            Id = "11111111-1111-1111-1111-111111111111",
            UserName = "customer@example.com",
            Email = "customer@example.com",
            FirstName = "John",
            LastName = "Doe",
            Phone = "123-456-7890"
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
            Level = TicketMasala.Domain.Common.EmployeeType.Support
        };

        context.Users.AddRange(customer, employee);
        await context.SaveChangesAsync();

        // Act
        var ticket = await service.CreateTicketAsync(
            description: "Assigned ticket",
            customerId: customer.Id,
            responsibleId: employee.Id,
            projectGuid: null,
            completionTarget: DateTime.UtcNow.AddDays(7)
        );

        // Assert
        Assert.NotNull(ticket);
        Assert.Equal(TicketMasala.Domain.Common.Status.Assigned, ticket.TicketStatus);
        Assert.Equal(employee.Id, ticket.ResponsibleId);
    }

    [Fact]
    public async Task CreateTicketAsync_WithInvalidCustomer_ThrowsException()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.CreateTicketAsync(
                description: "Test ticket",
                customerId: "invalid-id",
                responsibleId: null,
                projectGuid: null,
                completionTarget: DateTime.UtcNow.AddDays(7)
            )
        );
    }

    [Fact]
    public async Task GetTicketDetailsAsync_WithValidGuid_ReturnsViewModel()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        var customer = new ApplicationUser
        {
            Id = "11111111-1111-1111-1111-111111111111",
            UserName = "customer@example.com",
            Email = "customer@example.com",
            FirstName = "John",
            LastName = "Doe",
            Phone = "123-456-7890"
        };

        context.Users.Add(customer);

        var ticket = new Ticket
        {
            Description = "Test ticket",
            DomainId = "IT",
            Status = "New",
            Title = "Test Ticket",
            CustomFieldsJson = "{}",
            TicketStatus = TicketMasala.Domain.Common.Status.Pending,
            EstimatedEffortPoints = 5,
            PriorityScore = 10.5,

            CustomerId = customer.Id
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetTicketDetailsAsync(ticket.Guid);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ticket.Guid, result.Guid);
        Assert.Equal("Test ticket", result.Description);
        Assert.Equal("John Doe", result.CustomerName);
        Assert.Equal(5, result.EstimatedEffortPoints);
        Assert.Equal(10.5, result.PriorityScore);
    }

    [Fact]
    public async Task GetTicketDetailsAsync_WithInvalidGuid_ReturnsNull()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        // Act
        var result = await service.GetTicketDetailsAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AssignTicketAsync_WithValidData_AssignsTicket()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        var customer = new ApplicationUser
        {
            Id = "11111111-1111-1111-1111-111111111111",
            UserName = "customer@example.com",
            Email = "customer@example.com",
            FirstName = "John",
            LastName = "Doe",
            Phone = "123-456-7890"
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
            Level = TicketMasala.Domain.Common.EmployeeType.Support
        };

        context.Users.AddRange(customer, employee);

        var ticket = new Ticket
        {
            Description = "Unassigned ticket",
            DomainId = "IT",
            Status = "New",
            Title = "Test Ticket",
            CustomFieldsJson = "{}",
            TicketStatus = TicketMasala.Domain.Common.Status.Pending,
            CustomerId = customer.Id
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        // Act
        var result = await service.AssignTicketAsync(ticket.Guid, employee.Id);

        // Assert
        Assert.True(result);

        var updatedTicket = await context.Tickets.FindAsync(ticket.Guid);
        Assert.NotNull(updatedTicket);
        Assert.Equal(employee.Id, updatedTicket.ResponsibleId);
        Assert.Equal(TicketMasala.Domain.Common.Status.Assigned, updatedTicket.TicketStatus);
        Assert.Contains("AI-Assigned", updatedTicket.GerdaTags);
    }

    [Fact]
    public async Task AssignTicketAsync_WithInvalidTicket_ReturnsFalse()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        // Act
        var result = await service.AssignTicketAsync(Guid.NewGuid(), "some-agent-id");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AssignTicketAsync_WithInvalidAgent_ReturnsFalse()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        var customer = new ApplicationUser
        {
            Id = "11111111-1111-1111-1111-111111111111",
            UserName = "customer@example.com",
            Email = "customer@example.com",
            FirstName = "John",
            LastName = "Doe",
            Phone = "123-456-7890"
        };

        context.Users.Add(customer);

        var ticket = new Ticket
        {
            Description = "Test ticket",
            DomainId = "IT",
            Status = "New",
            Title = "Test Ticket",
            CustomFieldsJson = "{}",
            TicketStatus = TicketMasala.Domain.Common.Status.Pending,
            CustomerId = customer.Id
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        // Act
        var result = await service.AssignTicketAsync(ticket.Guid, "invalid-agent-id");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetCustomerSelectListAsync_ReturnsAllCustomers()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        context.Users.AddRange(
            new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "customer1@example.com",
                Email = "customer1@example.com",
                FirstName = "John",
                LastName = "Doe",
                Phone = "111111111"
            },
            new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "customer2@example.com",
                Email = "customer2@example.com",
                FirstName = "Jane",
                LastName = "Smith",
                Phone = "222222222"
            }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetCustomerSelectListAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Text == "John Doe");
        Assert.Contains(result, item => item.Text == "Jane Smith");
    }

    [Fact]
    public async Task GetEmployeeSelectListAsync_ReturnsAllEmployees()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        context.Users.AddRange(
            new Employee
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "emp1@example.com",
                Email = "emp1@example.com",
                FirstName = "Alice",
                LastName = "Johnson",
                Phone = "333333333",
                Team = "Support",
                Level = TicketMasala.Domain.Common.EmployeeType.Support
            },
            new Employee
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "emp2@example.com",
                Email = "emp2@example.com",
                FirstName = "Bob",
                LastName = "Williams",
                Phone = "444444444",
                Team = "Development",
                Level = TicketMasala.Domain.Common.EmployeeType.ProjectManager
            }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetEmployeeSelectListAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.Text == "Alice Johnson");
        Assert.Contains(result, item => item.Text == "Bob Williams");
    }
    [Fact]
    public void ParseCustomFields_ReturnsCorrectJson()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);

        // Re-create service with mock access since generic CreateService hides mocks
        // But we can just use CreateService and rely on the fact we can't easily access the inner mock 
        // UNLESS we refactor CreateService to take mocks or return them.
        // OR we just Instantiate TicketService manually here for this specific test to have full control over the Mock.

        var domainConfig = new Mock<IDomainConfigurationService>();
        var ticketRepo = new Mock<ITicketRepository>();
        var userRepo = new Mock<IUserRepository>();
        var projRepo = new Mock<IProjectRepository>();
        var logger = new Mock<ILogger<TicketService>>();
        var ruleEngine = new Mock<IRuleEngineService>();

        var dispatchLogger = new Mock<ILogger<TicketDispatchService>>();
        var reportingLogger = new Mock<ILogger<TicketReportingService>>();
        var notificationLogger = new Mock<ILogger<TicketNotificationService>>();
        var ticketDispatchService = new TicketDispatchService(ticketRepo.Object, dispatchLogger.Object);
        var ticketReportingService = new TicketReportingService(ticketRepo.Object, reportingLogger.Object);
        var ticketNotificationService = new TicketNotificationService(new Mock<INotificationService>().Object, notificationLogger.Object);
        var service = new TicketService(
            context,
            ticketRepo.Object,
            userRepo.Object,
            projRepo.Object,
            new List<ITicketObserver>(),
            new List<ICommentObserver>(),
            new Mock<INotificationService>().Object,
            new Mock<IAuditService>().Object,
            new Mock<IHttpContextAccessor>().Object,
            ruleEngine.Object,
            domainConfig.Object,
            logger.Object,
            ticketDispatchService,
            ticketReportingService,
            ticketNotificationService
        );

        // Mock Domain Config
        var fields = new List<CustomFieldDefinition>
            {
                new CustomFieldDefinition { Name = "Budget", Type = "Currency", Label = "Budget" },
                new CustomFieldDefinition { Name = "IsUrgent", Type = "Boolean", Label = "Urgent?" },
                new CustomFieldDefinition { Name = "Notes", Type = "String", Label = "Notes" }
            };
        domainConfig.Setup(d => d.GetCustomFields("IT")).Returns(fields);

        var formValues = new Dictionary<string, string>
            {
                { "customFields[Budget]", "500.50" },
                { "customFields[IsUrgent]", "true" },
                { "customFields[Notes]", "Some notes" },
                { "otherField", "ignore" }
            };

        // Act
        var json = service.ParseCustomFields("IT", formValues);

        // Assert
        Assert.Contains("\"Budget\":500.50", json); // Check numeric parsing
        Assert.Contains("\"IsUrgent\":true", json); // Check boolean parsing
        Assert.Contains("\"Notes\":\"Some notes\"", json);
    }
}
