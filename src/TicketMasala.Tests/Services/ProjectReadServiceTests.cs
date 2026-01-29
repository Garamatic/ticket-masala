using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web;
using TicketMasala.Domain.Data;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.ViewModels.Projects;
using TicketMasala.Web.ViewModels.Customers;
using Microsoft.AspNetCore.Identity;
using TicketMasala.Web.Abstractions;

namespace TicketMasala.Tests.Services;

public class ProjectReadServiceTests
{
    private readonly Mock<ILogger<ProjectReadService>> _mockLogger;
    private readonly DbContextOptions<MasalaDbContext> _dbOptions;

    public ProjectReadServiceTests()
    {
        _mockLogger = new Mock<ILogger<ProjectReadService>>();

        _dbOptions = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseInMemoryDatabase(databaseName: "TestProjectReadDb_" + Guid.NewGuid())
            .Options;
    }

    private ProjectReadService CreateService(MasalaDbContext context)
    {
        var mockProjectRepo = new Mock<IProjectRepository>();

        return new ProjectReadService(
            context,
            mockProjectRepo.Object,
            _mockLogger.Object,
            new Mock<ISystemClock>().Object
        );
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
    public async Task GetAllProjectsAsync_ReturnsAllProjects()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        var customer = CreateTestCustomer();
        context.Users.Add(customer);

        var project1 = new Project
        {
            Name = "Project 1",
            Description = "Description 1",
            Status = TicketMasala.Domain.Common.Status.Pending,
            Customer = customer
        };
        project1.Customers.Add(customer);

        var project2 = new Project
        {
            Name = "Project 2",
            Description = "Description 2",
            Status = TicketMasala.Domain.Common.Status.Assigned,
            Customer = customer
        };
        project2.Customers.Add(customer);

        context.Projects.AddRange(project1, project2);
        await context.SaveChangesAsync();

        // Act
        var results = await service.GetAllProjectsAsync(null, false);

        // Assert
        var projectList = results.ToList();
        Assert.Equal(2, projectList.Count);
        Assert.Contains(projectList, p => p.ProjectDetails.Name == "Project 1");
        Assert.Contains(projectList, p => p.ProjectDetails.Name == "Project 2");
    }

    [Fact]
    public async Task GetAllProjectsAsync_ForCustomer_FiltersCorrectly()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        var customer1 = CreateTestCustomer("1");
        var customer2 = CreateTestCustomer("2");
        context.Users.AddRange(customer1, customer2);

        var project1 = new Project
        {
            Name = "Customer1 Project",
            Description = "Description 1",
            Status = TicketMasala.Domain.Common.Status.Pending,
            Customer = customer1
        };
        project1.Customers.Add(customer1);

        var project2 = new Project
        {
            Name = "Customer2 Project",
            Description = "Description 2",
            Status = TicketMasala.Domain.Common.Status.Pending,
            Customer = customer2
        };
        project2.Customers.Add(customer2);

        context.Projects.AddRange(project1, project2);
        await context.SaveChangesAsync();

        // Act
        var results = await service.GetAllProjectsAsync(customer1.Id, isCustomer: true);

        // Assert
        var projectList = results.ToList();
        Assert.Single(projectList);
        Assert.Equal("Customer1 Project", projectList[0].ProjectDetails.Name);
    }

    [Fact]
    public async Task GetProjectDetailsAsync_WithValidGuid_ReturnsProject()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        var customer = CreateTestCustomer();
        context.Users.Add(customer);

        var project = new Project
        {
            Name = "Test Project",
            Description = "Test Description",
            Status = TicketMasala.Domain.Common.Status.InProgress,
            Customer = customer
        };

        context.Projects.Add(project);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetProjectDetailsAsync(project.Guid);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Project", result.ProjectDetails.Name);
        Assert.Equal("Test Description", result.ProjectDetails.Description);
        Assert.Equal(TicketMasala.Domain.Common.Status.InProgress, result.ProjectDetails.Status);
    }

    [Fact]
    public async Task GetProjectDetailsAsync_WithInvalidGuid_ReturnsNull()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        // Act
        var result = await service.GetProjectDetailsAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProjectForEditAsync_WithValidGuid_ReturnsViewModel()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        var customer = CreateTestCustomer();
        context.Users.Add(customer);

        var project = new Project
        {
            Name = "Edit Test Project",
            Description = "Edit Test Description",
            Status = TicketMasala.Domain.Common.Status.Pending,
            Customer = customer,
            CompletionTarget = DateTime.UtcNow.AddDays(30)
        };

        context.Projects.Add(project);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetProjectForEditAsync(project.Guid);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(project.Guid, result.Guid);
        Assert.Equal("Edit Test Project", result.Name);
        Assert.Equal("Edit Test Description", result.Description);
        Assert.Equal(customer.Id, result.SelectedCustomerId);
    }

    [Fact]
    public async Task GetProjectForEditAsync_WithInvalidGuid_ReturnsNull()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        // Act
        var result = await service.GetProjectForEditAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCustomerSelectListAsync_ReturnsAllCustomers()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        var customer1 = CreateTestCustomer("1");
        var customer2 = CreateTestCustomer("2");
        context.Users.AddRange(customer1, customer2);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetCustomerSelectListAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetTemplateSelectListAsync_ReturnsAllTemplates()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        context.ProjectTemplates.AddRange(
            new ProjectTemplate { Name = "Template 1", Description = "Desc 1" },
            new ProjectTemplate { Name = "Template 2", Description = "Desc 2" }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetTemplateSelectListAsync();

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, t => t.Text == "Template 1");
        Assert.Contains(result, t => t.Text == "Template 2");
    }

    [Fact]
    public async Task GetProjectDetailsAsync_WithSoftDeletedProject_ReturnsNull()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        var customer = CreateTestCustomer();
        context.Users.Add(customer);

        var project = new Project
        {
            Name = "Deleted Project",
            Description = "Description",
            Status = TicketMasala.Domain.Common.Status.Pending,
            Customer = customer,
            ValidUntil = DateTime.UtcNow.AddDays(-1) // Soft deleted
        };

        context.Projects.Add(project);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetProjectDetailsAsync(project.Guid);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SearchProjectsAsync_ReturnsMatchingProjects()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        var customer = CreateTestCustomer();
        context.Users.Add(customer);

        context.Projects.AddRange(
            new Project { Name = "Alpha Project", Description = "Desc 1", Status = TicketMasala.Domain.Common.Status.Pending, Customer = customer },
            new Project { Name = "Beta Project", Description = "Project Alpha related", Status = TicketMasala.Domain.Common.Status.Pending, Customer = customer },
            new Project { Name = "Gamma Project", Description = "Desc 2", Status = TicketMasala.Domain.Common.Status.Pending, Customer = customer }
        );
        await context.SaveChangesAsync();

        // Act
        var results = await service.SearchProjectsAsync("Alpha");

        // Assert
        var list = results.ToList();
        Assert.Equal(2, list.Count);
        Assert.Contains(list, p => p.ProjectDetails.Name == "Alpha Project");
        Assert.Contains(list, p => p.ProjectDetails.Name == "Beta Project"); // Matched by description
    }

    [Fact]
    public async Task GetProjectStatisticsAsync_ReturnsCorrectStats()
    {
        // Arrange
        using var context = new MasalaDbContext(_dbOptions);
        var service = CreateService(context);

        var customer = CreateTestCustomer();
        context.Users.Add(customer);

        var p1 = new Project { Name = "P1", Description = "Desc 1", Status = TicketMasala.Domain.Common.Status.InProgress, Customer = customer };
        p1.Tasks.Add(new Ticket { TicketStatus = TicketMasala.Domain.Common.Status.Completed });
        p1.Tasks.Add(new Ticket { TicketStatus = TicketMasala.Domain.Common.Status.InProgress });

        var p2 = new Project { Name = "P2", Description = "Desc 2", Status = TicketMasala.Domain.Common.Status.Completed, Customer = customer };

        var p3 = new Project { Name = "P3", Description = "Desc 3", Status = TicketMasala.Domain.Common.Status.Pending, Customer = customer };

        context.Projects.AddRange(p1, p2, p3);
        await context.SaveChangesAsync();

        // Act
        var stats = await service.GetProjectStatisticsAsync(customer.Id);

        // Assert
        Assert.Equal(3, stats.TotalProjects);
        Assert.Equal(1, stats.ActiveProjects);
        Assert.Equal(1, stats.CompletedProjects);
        Assert.Equal(1, stats.PendingProjects);
        Assert.Equal(2, stats.TotalTasks);
        Assert.Equal(1, stats.CompletedTasks);
    }
}
