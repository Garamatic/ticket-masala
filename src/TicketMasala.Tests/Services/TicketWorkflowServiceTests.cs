using TicketMasala.Web.Engine.GERDA.Tickets;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Observers;
using TicketMasala.Domain.Data;
using Microsoft.AspNetCore.Http;
using TicketMasala.Web.Engine.Compiler;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Web.Engine.Security;

namespace TicketMasala.Tests.Services;

public class TicketWorkflowServiceTests
{
    private readonly DbContextOptions<MasalaDbContext> _dbOptions;

    public TicketWorkflowServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseInMemoryDatabase(databaseName: "TestTicketWorkflowDb_" + Guid.NewGuid())
            .Options;
    }

    private TicketWorkflowService CreateService(MasalaDbContext context)
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
        var logger = new Mock<ILogger<TicketWorkflowService>>();
        var piiScrubber = new Mock<IPiiScrubberService>();
        var ticketDispatchService = new Mock<TicketMasala.Web.Engine.GERDA.Tickets.Domain.TicketDispatchService>(ticketRepository.Object, new Mock<ILogger<TicketMasala.Web.Engine.GERDA.Tickets.Domain.TicketDispatchService>>().Object);
        var ticketNotificationService = new Mock<TicketMasala.Web.Engine.GERDA.Tickets.Domain.TicketNotificationService>(notificationService.Object, new Mock<ILogger<TicketMasala.Web.Engine.GERDA.Tickets.Domain.TicketNotificationService>>().Object);

        piiScrubber.Setup(s => s.Scrub(It.IsAny<string>())).Returns((string s) => s);

        userRepository.Setup(r => r.GetCustomerByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => context.Users.OfType<ApplicationUser>().FirstOrDefault(u => u.Id == id));

        userRepository.Setup(r => r.GetEmployeeByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => context.Users.OfType<Employee>().FirstOrDefault(u => u.Id == id));

        ticketRepository.Setup(r => r.AddAsync(It.IsAny<Ticket>()))
           .Callback((Ticket t) => context.Tickets.Add(t))
           .ReturnsAsync((Ticket t) => t);

        ticketRepository.Setup(r => r.UpdateAsync(It.IsAny<Ticket>()))
            .Callback((Ticket t) =>
            {
                var existing = context.Tickets.Find(t.Guid);
                if (existing != null) context.Entry(existing).CurrentValues.SetValues(t);
            })
            .Returns(Task.CompletedTask);

        projectRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()))
            .ReturnsAsync((Guid id, bool include) => context.Projects.Include(p => p.Tasks).FirstOrDefault(p => p.Guid == id));

        return new TicketWorkflowService(
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
            piiScrubber.Object,
            ticketNotificationService.Object,
            logger.Object,
            ticketDispatchService.Object
        );
    }

    [Fact]
    public async Task CreateTicketAsync_WithValidData_CreatesTicket()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);
        var customer = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "cust@test.com", Email = "cust@test.com" };
        context.Users.Add(customer);
        await context.SaveChangesAsync();

        // Act
        var ticket = await service.CreateTicketAsync("Test ticket", customer.Id, null, null, null);

        // Assert
        Assert.NotNull(ticket);
        Assert.Equal("Test ticket", ticket.Description);
        Assert.Equal(Status.Pending, ticket.TicketStatus);
    }

    [Fact]
    public async Task CreateTicketAsync_WithResponsible_SetsStatusToAssigned()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);
        var customer = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "cust@test.com", Email = "cust@test.com" };
        var employee = new Employee { Id = Guid.NewGuid().ToString(), UserName = "emp@test.com", Email = "emp@test.com" };
        context.Users.AddRange(customer, employee);
        await context.SaveChangesAsync();

        // Act
        var ticket = await service.CreateTicketAsync("Assigned ticket", customer.Id, employee.Id, null, null);

        // Assert
        Assert.Equal(Status.Assigned, ticket.TicketStatus);
        Assert.Equal(employee.Id, ticket.ResponsibleId);
    }
}
