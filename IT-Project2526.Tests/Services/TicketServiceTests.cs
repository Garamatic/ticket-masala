using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using IT_Project2526.Services;
using IT_Project2526.Models;
using IT_Project2526.Repositories;
using IT_Project2526.Observers;
using Microsoft.AspNetCore.Http;
using IT_Project2526;
using System.Security.Claims;

namespace IT_Project2526.Tests.Services
{
    public class TicketServiceTests
    {
        private readonly Mock<ILogger<TicketService>> _mockLogger;
        private readonly DbContextOptions<ITProjectDB> _dbOptions;

        public TicketServiceTests()
        {
            _mockLogger = new Mock<ILogger<TicketService>>();
            
            // Use in-memory database for testing
            _dbOptions = new DbContextOptionsBuilder<ITProjectDB>()
                .UseInMemoryDatabase(databaseName: "TestTicketDb_" + Guid.NewGuid())
                .Options;
        }

        private TicketService CreateService(ITProjectDB context)
        {
            var ticketRepo = new EfCoreTicketRepository(context, new Mock<ILogger<EfCoreTicketRepository>>().Object);
            var userRepo = new EfCoreUserRepository(context, new Mock<ILogger<EfCoreUserRepository>>().Object);
            var projectRepo = new EfCoreProjectRepository(context, new Mock<ILogger<EfCoreProjectRepository>>().Object);
            
            var observers = new List<ITicketObserver>();
            var notificationService = new Mock<INotificationService>();
            var auditService = new Mock<IAuditService>();
            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            
            // Setup HttpContext
            var httpContext = new DefaultHttpContext();
            httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

            return new TicketService(
                context,
                ticketRepo,
                userRepo,
                projectRepo,
                observers,
                notificationService.Object,
                auditService.Object,
                httpContextAccessor.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task CreateTicketAsync_WithValidData_CreatesTicket()
        {
            // Arrange
            using var context = new ITProjectDB(_dbOptions);
            var service = CreateService(context);
            
            var customer = new Customer 
            { 
                Id = Guid.NewGuid().ToString(),
                UserName = "customer@example.com",
                Email = "customer@example.com",
                FirstName = "Test",
                LastName = "Customer",
                Phone = "123456789"
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
            Assert.Equal(customer.Id, ticket.Customer.Id);
            Assert.Equal(Status.Pending, ticket.TicketStatus);
            Assert.Null(ticket.ResponsibleId);
        }

        [Fact]
        public async Task CreateTicketAsync_WithResponsible_SetsStatusToAssigned()
        {
            // Arrange
            using var context = new ITProjectDB(_dbOptions);
            var service = CreateService(context);
            
            var customer = new Customer 
            { 
                Id = Guid.NewGuid().ToString(),
                UserName = "customer@example.com",
                Email = "customer@example.com",
                FirstName = "Test",
                LastName = "Customer",
                Phone = "123456789"
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
            using var context = new ITProjectDB(_dbOptions);
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
            using var context = new ITProjectDB(_dbOptions);
            var service = CreateService(context);
            
            var customer = new Customer 
            { 
                Id = Guid.NewGuid().ToString(),
                UserName = "customer@example.com",
                Email = "customer@example.com",
                FirstName = "John",
                LastName = "Doe",
                Phone = "123456789"
            };
            
            context.Users.Add(customer);
            
            var ticket = new Ticket
            {
                Description = "Test ticket",
                Customer = customer,
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
            using var context = new ITProjectDB(_dbOptions);
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
            using var context = new ITProjectDB(_dbOptions);
            var service = CreateService(context);
            
            var customer = new Customer 
            { 
                Id = Guid.NewGuid().ToString(),
                UserName = "customer@example.com",
                Email = "customer@example.com",
                FirstName = "Test",
                LastName = "Customer",
                Phone = "123456789"
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
                Customer = customer,
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
            using var context = new ITProjectDB(_dbOptions);
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
            using var context = new ITProjectDB(_dbOptions);
            var service = CreateService(context);
            
            var customer = new Customer 
            { 
                Id = Guid.NewGuid().ToString(),
                UserName = "customer@example.com",
                Email = "customer@example.com",
                FirstName = "Test",
                LastName = "Customer",
                Phone = "123456789"
            };
            
            context.Users.Add(customer);
            
            var ticket = new Ticket
            {
                Description = "Test ticket",
                Customer = customer,
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
            using var context = new ITProjectDB(_dbOptions);
            var service = CreateService(context);
            
            context.Users.AddRange(
                new Customer 
                { 
                    Id = Guid.NewGuid().ToString(),
                    UserName = "customer1@example.com",
                    Email = "customer1@example.com",
                    FirstName = "John",
                    LastName = "Doe",
                    Phone = "111111111"
                },
                new Customer 
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
            using var context = new ITProjectDB(_dbOptions);
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
}
