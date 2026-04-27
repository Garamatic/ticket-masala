using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Web;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.AI;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.Observers;
using TicketMasala.Domain.Repositories;
using TicketMasala.Web.ViewModels.Projects;
using Xunit;

namespace TicketMasala.Tests.Services;

public class ProjectWorkflowServiceTests
{
    private readonly Mock<ILogger<ProjectWorkflowService>> _mockLogger;
    private readonly DbContextOptions<MasalaDbContext> _dbOptions;

    public ProjectWorkflowServiceTests()
    {
        _mockLogger = new Mock<ILogger<ProjectWorkflowService>>();

        _dbOptions = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseInMemoryDatabase(databaseName: "TestProjectWorkflowDb_" + Guid.NewGuid())
            .Options;
    }

    private ProjectWorkflowService CreateService(MasalaDbContext context)
    {
        var mockProjectRepo = new Mock<IProjectRepository>();
        var mockUserManager = MockUserManager();
        var mockObservers = new List<IProjectObserver>();
        var mockOpenAiService = new Mock<IOpenAiService>();
        var mockTemplateService = new Mock<IProjectTemplateService>();
        var mockClock = new Mock<ISystemClock>();

        return new ProjectWorkflowService(
            context,
            mockProjectRepo.Object,
            mockUserManager.Object,
            mockObservers,
            mockOpenAiService.Object,
            mockTemplateService.Object,
            _mockLogger.Object,
            mockClock.Object
        );
    }

    private Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        return mockUserManager;
    }

    private ApplicationUser CreateTestCustomer(string suffix = "")
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"customer{suffix}@example.com",
            Email = $"customer{suffix}@example.com",
            FirstName = "Test",
            LastName = "Customer" + suffix,
            Phone = "123456789"
        };
    }

    [Fact]
    public async Task UpdateProjectAsync_WithValidData_UpdatesProject()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        var customer = CreateTestCustomer();
        context.Users.Add(customer);

        var project = new Project
        {
            Name = "Original Name",
            Description = "Original Description",
            Status = TicketMasala.Domain.Common.Status.Pending,
            Customer = customer
        };

        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var updateViewModel = new NewProject
        {
            Guid = project.Guid,
            Name = "Updated Name",
            Description = "Updated Description",
            SelectedCustomerId = customer.Id,
            CreationDate = DateTime.UtcNow.AddDays(60)
        };

        // Act
        var result = await service.UpdateProjectAsync(project.Guid, updateViewModel);

        // Assert
        Assert.True(result);

        var updatedProject = await context.Projects.FindAsync(project.Guid);
        Assert.NotNull(updatedProject);
        Assert.Equal("Updated Name", updatedProject.Name);
        Assert.Equal("Updated Description", updatedProject.Description);
    }

    [Fact]
    public async Task UpdateProjectAsync_WithInvalidGuid_ReturnsFalse()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        var updateViewModel = new NewProject
        {
            Guid = Guid.NewGuid(),
            Name = "Updated Name",
            Description = "Updated Description"
        };

        // Act
        var result = await service.UpdateProjectAsync(Guid.NewGuid(), updateViewModel);

        // Assert
        Assert.False(result);
    }
}
