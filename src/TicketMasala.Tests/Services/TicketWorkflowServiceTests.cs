using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Engine.GERDA.Tickets;
using Xunit;

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
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var resolutionService = new Mock<ITicketResolutionService>();
        var commentService = new Mock<ITicketCommentService>();
        var reviewService = new Mock<ITicketReviewService>();
        var timeLoggingService = new Mock<ITicketTimeLoggingService>();
        var creationService = new Mock<ITicketCreationService>();
        // Setup mock to create actual tickets
        creationService.Setup(s => s.CreateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<string>()))
            .ReturnsAsync((string desc, string custId, string? respId, Guid? proj, DateTime? comp, string? creator) =>
            {
                var ticket = new Ticket
                {
                    Guid = Guid.NewGuid(),
                    Description = desc,
                    CustomerId = custId,
                    ResponsibleId = respId,
                    ProjectGuid = proj,
                    Title = desc.Length > 50 ? desc[..47] + "..." : desc,
                    TicketStatus = !string.IsNullOrEmpty(respId) ? Status.Assigned : Status.Pending
                };
                return ticket;
            });
        var updateService = new Mock<ITicketUpdateService>();
        var assignmentFacade = new Mock<ITicketAssignmentFacade>();

        // No additional setup needed - all behavior is in the specialized services

        return new TicketWorkflowService(
            httpContextAccessor.Object,
            resolutionService.Object,
            commentService.Object,
            reviewService.Object,
            timeLoggingService.Object,
            creationService.Object,
            updateService.Object,
            assignmentFacade.Object
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
