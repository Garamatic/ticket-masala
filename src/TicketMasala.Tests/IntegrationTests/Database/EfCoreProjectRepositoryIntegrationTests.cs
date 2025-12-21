using Microsoft.EntityFrameworkCore;
using TicketMasala.Tests.TestHelpers;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using Xunit;

namespace TicketMasala.Tests.IntegrationTests.Database;

/// <summary>
/// Integration tests for EfCoreProjectRepository using real SQLite database.
/// </summary>
public class EfCoreProjectRepositoryIntegrationTests : IDisposable
{
    private readonly DatabaseTestFixture _fixture;

    public EfCoreProjectRepositoryIntegrationTests()
    {
        _fixture = new DatabaseTestFixture();
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidProject_CreatesProjectInDatabase()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        var project = new Project
        {
            Name = "New Test Project",
            Description = "Test Project Description",
            Status = TicketMasala.Domain.Common.Status.Pending,
            CustomerId = customer.Id,
            CreatorGuid = Guid.NewGuid(),
            CreationDate = DateTime.UtcNow
        };

        // Act
        var result = await _fixture.ProjectRepository.AddAsync(project);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Guid);

        var fromDb = await _fixture.Context.Projects.FindAsync(result.Guid);
        Assert.NotNull(fromDb);
        Assert.Equal("New Test Project", fromDb.Name);
    }

    [Fact]
    public async Task AddAsync_WithProjectManager_PersistsRelation()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        var manager = await _fixture.SeedTestEmployeeAsync(level: EmployeeType.ProjectManager);

        var project = new Project
        {
            Name = "Managed Project",
            Description = "Project with manager",
            Status = TicketMasala.Domain.Common.Status.InProgress,
            CustomerId = customer.Id,
            ProjectManagerId = manager.Id,
            CreatorGuid = Guid.NewGuid(),
            CreationDate = DateTime.UtcNow
        };

        // Act
        await _fixture.ProjectRepository.AddAsync(project);

        // Assert
        _fixture.Context.ChangeTracker.Clear();
        var fromDb = await _fixture.Context.Projects
            .Include(p => p.ProjectManager)
            .FirstAsync(p => p.Guid == project.Guid);

        Assert.NotNull(fromDb.ProjectManager);
        Assert.Equal(manager.Id, fromDb.ProjectManagerId);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsProject()
    {
        // Arrange
        var project = await _fixture.SeedTestProjectAsync();

        // Act
        var result = await _fixture.ProjectRepository.GetByIdAsync(project.Guid);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(project.Guid, result.Guid);
        Assert.Equal("Test Project", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithIncludeRelations_LoadsManagerAndCustomer()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        var manager = await _fixture.SeedTestEmployeeAsync(level: EmployeeType.ProjectManager);
        var project = await _fixture.SeedTestProjectAsync(customer: customer, projectManager: manager);

        _fixture.Context.ChangeTracker.Clear();

        // Act
        var result = await _fixture.ProjectRepository.GetByIdAsync(project.Guid, includeRelations: true);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Customer);
        Assert.NotNull(result.ProjectManager);
        Assert.Equal(customer.Id, result.CustomerId);
        Assert.Equal(manager.Id, result.ProjectManagerId);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _fixture.ProjectRepository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllActiveProjects()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        await _fixture.SeedTestProjectAsync(customer: customer, status: TicketMasala.Domain.Common.Status.InProgress);
        await _fixture.SeedTestProjectAsync(customer: customer, status: TicketMasala.Domain.Common.Status.Pending);

        // Act
        var projects = await _fixture.ProjectRepository.GetAllAsync();

        // Assert
        Assert.Equal(2, projects.Count());
    }

    [Fact]
    public async Task GetAllAsync_ExcludesDeletedProjects()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        var activeProject = await _fixture.SeedTestProjectAsync(customer: customer);
        var deletedProject = await _fixture.SeedTestProjectAsync(customer: customer);
        deletedProject.ValidUntil = DateTime.UtcNow; // Soft-deleted
        await _fixture.Context.SaveChangesAsync();

        // Act
        var projects = await _fixture.ProjectRepository.GetAllAsync();

        // Assert
        Assert.Single(projects);
        Assert.Equal(activeProject.Guid, projects.First().Guid);
    }

    #endregion

    #region GetActiveProjectsAsync Tests

    [Fact]
    public async Task GetActiveProjectsAsync_FiltersInactiveProjects()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        await _fixture.SeedTestProjectAsync(customer: customer, status: TicketMasala.Domain.Common.Status.Pending);
        await _fixture.SeedTestProjectAsync(customer: customer, status: TicketMasala.Domain.Common.Status.InProgress);
        await _fixture.SeedTestProjectAsync(customer: customer, status: TicketMasala.Domain.Common.Status.Completed);
        await _fixture.SeedTestProjectAsync(customer: customer, status: TicketMasala.Domain.Common.Status.Cancelled);

        // Act
        var activeProjects = await _fixture.ProjectRepository.GetActiveProjectsAsync();

        // Assert
        Assert.Equal(2, activeProjects.Count());
        Assert.All(activeProjects, p =>
            Assert.True(p.Status == TicketMasala.Domain.Common.Status.Pending || p.Status == TicketMasala.Domain.Common.Status.InProgress));
    }

    #endregion

    #region GetByCustomerIdAsync Tests

    [Fact]
    public async Task GetByCustomerIdAsync_ReturnsOnlyCustomerProjects()
    {
        // Arrange
        var customer1 = await _fixture.SeedTestCustomerAsync();
        var customer2 = await _fixture.SeedTestCustomerAsync();

        await _fixture.SeedTestProjectAsync(customer: customer1);
        await _fixture.SeedTestProjectAsync(customer: customer1);
        await _fixture.SeedTestProjectAsync(customer: customer2);

        // Act
        var projects = await _fixture.ProjectRepository.GetByCustomerIdAsync(customer1.Id);

        // Assert
        Assert.Equal(2, projects.Count());
        Assert.All(projects, p => Assert.Equal(customer1.Id, p.CustomerId));
    }

    #endregion

    #region GetRecommendedProjectForCustomerAsync Tests

    [Fact]
    public async Task GetRecommendedProjectForCustomerAsync_ReturnsLatestActiveProject()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();

        var oldProject = new Project
        {
            Name = "Old Project",
            Description = "Created first",
            Status = TicketMasala.Domain.Common.Status.InProgress,
            CustomerId = customer.Id,
            CreatorGuid = Guid.NewGuid(),
            CreationDate = DateTime.UtcNow.AddDays(-10)
        };
        _fixture.Context.Projects.Add(oldProject);

        var newProject = new Project
        {
            Name = "New Project",
            Description = "Created later",
            Status = TicketMasala.Domain.Common.Status.InProgress,
            CustomerId = customer.Id,
            CreatorGuid = Guid.NewGuid(),
            CreationDate = DateTime.UtcNow
        };
        _fixture.Context.Projects.Add(newProject);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var recommended = await _fixture.ProjectRepository.GetRecommendedProjectForCustomerAsync(customer.Id);

        // Assert
        Assert.NotNull(recommended);
        Assert.Equal("New Project", recommended.Name);
    }

    [Fact]
    public async Task GetRecommendedProjectForCustomerAsync_ExcludesCompletedProjects()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        await _fixture.SeedTestProjectAsync(customer: customer, status: TicketMasala.Domain.Common.Status.Completed);

        // Act
        var recommended = await _fixture.ProjectRepository.GetRecommendedProjectForCustomerAsync(customer.Id);

        // Assert
        Assert.Null(recommended);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ModifiesExistingProject()
    {
        // Arrange
        var project = await _fixture.SeedTestProjectAsync();
        project.Name = "Updated Project Name";
        project.Status = TicketMasala.Domain.Common.Status.Completed;

        // Act
        await _fixture.ProjectRepository.UpdateAsync(project);

        // Assert
        _fixture.Context.ChangeTracker.Clear();
        var fromDb = await _fixture.Context.Projects.FindAsync(project.Guid);
        Assert.NotNull(fromDb);
        Assert.Equal("Updated Project Name", fromDb.Name);
        Assert.Equal(TicketMasala.Domain.Common.Status.Completed, fromDb.Status);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_RemovesProjectFromDatabase()
    {
        // Arrange
        var project = await _fixture.SeedTestProjectAsync();
        var projectGuid = project.Guid;

        // Act
        await _fixture.ProjectRepository.DeleteAsync(projectGuid);

        // Assert
        var fromDb = await _fixture.Context.Projects.FindAsync(projectGuid);
        Assert.Null(fromDb);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingProject_ReturnsTrue()
    {
        // Arrange
        var project = await _fixture.SeedTestProjectAsync();

        // Act
        var exists = await _fixture.ProjectRepository.ExistsAsync(project.Guid);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingProject_ReturnsFalse()
    {
        // Act
        var exists = await _fixture.ProjectRepository.ExistsAsync(Guid.NewGuid());

        // Assert
        Assert.False(exists);
    }

    #endregion
}
