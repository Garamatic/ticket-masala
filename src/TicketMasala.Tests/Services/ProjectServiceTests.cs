using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web;
using TicketMasala.Web.Data;
using TicketMasala.Web.Services.Core;
using TicketMasala.Web.Services.Tickets;
using TicketMasala.Web.Services.Projects;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Services.Background;
using TicketMasala.Web.Models;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Observers;
using TicketMasala.Web.ViewModels.Projects;
using TicketMasala.Web.ViewModels.Customers;
using Microsoft.AspNetCore.Identity;
using TicketMasala.Web.Data;

namespace TicketMasala.Tests.Services;
    public class ProjectServiceTests
    {
        private readonly Mock<ILogger<ProjectService>> _mockLogger;
        private readonly DbContextOptions<MasalaDbContext> _dbOptions;

        public ProjectServiceTests()
        {
            _mockLogger = new Mock<ILogger<ProjectService>>();
            
            _dbOptions = new DbContextOptionsBuilder<MasalaDbContext>()
                .UseInMemoryDatabase(databaseName: "TestProjectDb_" + Guid.NewGuid())
                .Options;
        }

        private ProjectService CreateService(MasalaDbContext context)
        {
            var mockProjectRepo = new Mock<IProjectRepository>();
            var mockUserManager = MockUserManager();
            var mockObservers = new List<IProjectObserver>();
            
            return new ProjectService(
                context,
                mockProjectRepo.Object,
                mockUserManager.Object,
                mockObservers,
                _mockLogger.Object
            );
        }

        private Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);
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
                Status = Status.Pending,
                Customer = customer
            };
            project1.Customers.Add(customer);

            var project2 = new Project
            {
                Name = "Project 2",
                Description = "Description 2",
                Status = Status.Assigned,
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
                Status = Status.Pending,
                Customer = customer1
            };
            project1.Customers.Add(customer1);

            var project2 = new Project
            {
                Name = "Customer2 Project",
                Description = "Description 2",
                Status = Status.Pending,
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
                Status = Status.InProgress,
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
            Assert.Equal(Status.InProgress, result.ProjectDetails.Status);
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
                Status = Status.Pending,
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
                Status = Status.Pending,
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
                Status = Status.Pending,
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
}
