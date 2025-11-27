#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IT_Project2526.Models;
using IT_Project2526.Repositories;
using IT_Project2526.Services;
using IT_Project2526.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace IT_Project2526.Tests.Services
{
    public class ProjectServiceTests
    {
        private readonly Mock<IProjectRepository> _mockProjectRepo;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<ProjectService>> _mockLogger;
        private readonly IMemoryCache _cache;
        private readonly ProjectService _projectService;

        public ProjectServiceTests()
        {
            _mockProjectRepo = new Mock<IProjectRepository>();
            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<ProjectService>>();
            
            // Create real MemoryCache for testing
            _cache = new MemoryCache(new MemoryCacheOptions());
            
            // Mock UserManager (requires store mock)
            var store = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            _projectService = new ProjectService(
                _mockProjectRepo.Object,
                _mockUserManager.Object,
                _mockEmailService.Object,
                _mockLogger.Object,
                _cache);
        }

        [Fact]
        public async Task GetAllProjectsAsync_ReturnsProjectViewModels()
        {
            // Arrange
            var projects = new List<Project>
            {
                new Project
                {
                    Guid = Guid.NewGuid(),
                    Name = "Test Project",
                    Description = "Test Description",
                    Status = Status.Pending,
                    Tasks = new List<Ticket>()
                }
            };

            _mockProjectRepo
                .Setup(r => r.GetAllWithDetailsAsync())
                .ReturnsAsync(projects);

            // Act
            var result = await _projectService.GetAllProjectsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            var project = result.First();
            Assert.Equal("Test Project", project.ProjectDetails.Name);
        }

        [Fact]
        public async Task GetProjectByIdAsync_WithValidId_ReturnsProject()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var project = new Project
            {
                Guid = projectId,
                Name = "Test Project",
                Description = "Test Description",
                Status = Status.Pending,
                Tasks = new List<Ticket>()
            };

            _mockProjectRepo
                .Setup(r => r.GetByIdWithDetailsAsync(projectId))
                .ReturnsAsync(project);

            // Act
            var result = await _projectService.GetProjectByIdAsync(projectId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Project", result.ProjectDetails.Name);
        }

        [Fact]
        public async Task GetProjectByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            _mockProjectRepo
                .Setup(r => r.GetByIdWithDetailsAsync(projectId))
                .ReturnsAsync((Project?)null);

            // Act
            var result = await _projectService.GetProjectByIdAsync(projectId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateProjectAsync_WithExistingCustomer_CreatesProject()
        {
            // Arrange
            var customerId = "customer-123";
            var customer = new Customer
            {
                Id = customerId,
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "123-456-7890",
                UserName = "john@example.com"
            };

            var model = new NewProject
            {
                Name = "New Project",
                Description = "Project Description",
                IsNewCustomer = false,
                SelectedCustomerId = customerId
            };

            _mockUserManager
                .Setup(um => um.FindByIdAsync(customerId))
                .ReturnsAsync(customer);

            _mockProjectRepo
                .Setup(r => r.AddAsync(It.IsAny<Project>()))
                .ReturnsAsync((Project p) => p);

            _mockProjectRepo
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _projectService.CreateProjectAsync(model, "user-id");

            // Assert
            Assert.NotEqual(Guid.Empty, result);
            _mockProjectRepo.Verify(r => r.AddAsync(It.IsAny<Project>()), Times.Once);
            _mockProjectRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockEmailService.Verify(e => e.SendProjectAssignmentEmailAsync(
                It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DeleteProjectAsync_DeletesProject()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            _mockProjectRepo
                .Setup(r => r.DeleteAsync(projectId))
                .Returns(Task.CompletedTask);

            _mockProjectRepo
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            await _projectService.DeleteProjectAsync(projectId);

            // Assert
            _mockProjectRepo.Verify(r => r.DeleteAsync(projectId), Times.Once);
            _mockProjectRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateProjectStatusAsync_UpdatesStatusAndSetsCompletionDate_WhenCompleted()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var project = new Project
            {
                Guid = projectId,
                Name = "Test Project",
                Description = "Description",
                Status = Status.InProgress
            };

            _mockProjectRepo
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _mockProjectRepo
                .Setup(r => r.UpdateAsync(It.IsAny<Project>()))
                .Returns(Task.CompletedTask);

            _mockProjectRepo
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            await _projectService.UpdateProjectStatusAsync(projectId, Status.Completed);

            // Assert
            Assert.Equal(Status.Completed, project.Status);
            Assert.NotNull(project.CompletionDate);
            _mockProjectRepo.Verify(r => r.UpdateAsync(project), Times.Once);
        }

        [Fact]
        public async Task AssignProjectManagerAsync_AssignsManagerAndSendsEmail()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var managerId = "manager-123";
            
            var project = new Project
            {
                Guid = projectId,
                Name = "Test Project",
                Description = "Description",
                Status = Status.Pending
            };

            var manager = new Employee
            {
                Id = managerId,
                FirstName = "Manager",
                LastName = "User",
                Email = "manager@example.com",
                Phone = "123-456-7890",
                UserName = "manager@example.com",
                Team = "Development",
                Level = EmployeeType.ProjectManager
            };

            _mockProjectRepo
                .Setup(r => r.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _mockUserManager
                .Setup(um => um.FindByIdAsync(managerId))
                .ReturnsAsync(manager);

            _mockProjectRepo
                .Setup(r => r.UpdateAsync(It.IsAny<Project>()))
                .Returns(Task.CompletedTask);

            _mockProjectRepo
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            await _projectService.AssignProjectManagerAsync(projectId, managerId);

            // Assert
            Assert.Equal(managerId, project.ProjectManagerId);
            Assert.Equal(manager, project.ProjectManager);
            _mockEmailService.Verify(e => e.SendProjectAssignmentEmailAsync(
                manager.Email!, project.Name), Times.Once);
        }

        [Fact]
        public async Task SearchProjectsAsync_ReturnsMatchingProjects()
        {
            // Arrange
            var searchTerm = "Test";
            var projects = new List<Project>
            {
                new Project
                {
                    Guid = Guid.NewGuid(),
                    Name = "Test Project",
                    Description = "Description",
                    Status = Status.Pending,
                    Tasks = new List<Ticket>()
                }
            };

            _mockProjectRepo
                .Setup(r => r.SearchByNameAsync(searchTerm))
                .ReturnsAsync(projects);

            // Act
            var result = await _projectService.SearchProjectsAsync(searchTerm);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetCustomerStatisticsAsync_ReturnsStatistics()
        {
            // Arrange
            var customerId = "customer-123";
            var stats = new ProjectStatistics
            {
                TotalProjects = 10,
                ActiveProjects = 3,
                CompletedProjects = 5,
                PendingProjects = 2,
                InProgressProjects = 3
            };

            _mockProjectRepo
                .Setup(r => r.GetCustomerStatisticsAsync(customerId))
                .ReturnsAsync(stats);

            // Act
            var result = await _projectService.GetCustomerStatisticsAsync(customerId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.TotalProjects);
            Assert.Equal(5, result.CompletedProjects);
        }
    }
}
