using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Services.Core;
using TicketMasala.Web.Services.Tickets;
using TicketMasala.Web.Services.Projects;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Services.Background;
using TicketMasala.Web.Models;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Observers;
using Microsoft.AspNetCore.Http;
using TicketMasala.Web;
using System.Security.Claims;
using TicketMasala.Web.Engine.Compiler;
using TicketMasala.Web.Data;

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

            // Wire up TicketRepository Mocks
            ticketRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()))
                .ReturnsAsync((Guid id, bool includeRelations) => context.Tickets.Find(id));
            
            ticketRepository.Setup(r => r.UpdateAsync(It.IsAny<Ticket>()))
                .Callback((Ticket t) => {
                    var existing = context.Tickets.Find(t.Guid);
                    if (existing != null)
                    {
                        context.Entry(existing).CurrentValues.SetValues(t);
                    }
                })
                .Returns(Task.CompletedTask);

             ticketRepository.Setup(r => r.AddAsync(It.IsAny<Ticket>()))
                .Callback((Ticket t) => {
                    context.Tickets.Add(t);
                })
                .ReturnsAsync((Ticket t) => t);

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
                logger.Object);
        }

        [Fact]
        public async Task CreateTicketAsync_WithValidData_CreatesTicket()
        {
            // Arrange
            using var context = new MasalaDbContext(_dbOptions);
            var service = CreateService(context);
            
            var customer = new ApplicationUser
            {
                Id = "customer-id",
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
            Assert.Equal(Status.Pending, ticket.TicketStatus);
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
                Id = "customer-id",
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
                Level = EmployeeType.Support
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
            Assert.Equal(Status.Assigned, ticket.TicketStatus);
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
                Id = "customer-id",
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
                TicketStatus = Status.Pending,
                EstimatedEffortPoints = 5,
                PriorityScore = 10.5
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
                Id = "customer-id",
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
                Level = EmployeeType.Support
            };
            
            context.Users.AddRange(customer, employee);
            
            var ticket = new Ticket
            {
                Description = "Unassigned ticket",
                DomainId = "IT",
                Status = "New",
                Title = "Test Ticket",
                CustomFieldsJson = "{}",
                TicketStatus = Status.Pending
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
            Assert.Equal(Status.Assigned, updatedTicket.TicketStatus);
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
                Id = "customer-id",
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
                TicketStatus = Status.Pending
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
                    Level = EmployeeType.Support
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
                    Level = EmployeeType.ProjectManager
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
}
