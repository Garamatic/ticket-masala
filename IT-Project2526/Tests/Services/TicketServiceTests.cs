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
using Moq;
using Xunit;

namespace IT_Project2526.Tests.Services
{
    public class TicketServiceTests
    {
        private readonly Mock<ITicketRepository> _mockTicketRepo;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<TicketService>> _mockLogger;
        private readonly TicketService _ticketService;

        public TicketServiceTests()
        {
            _mockTicketRepo = new Mock<ITicketRepository>();
            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<TicketService>>();
            
            var store = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            _ticketService = new TicketService(
                _mockTicketRepo.Object,
                _mockUserManager.Object,
                _mockEmailService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GetAllTicketsAsync_ReturnsTicketViewModels()
        {
            // Arrange
            var customer = new Customer
            {
                Id = "cust-1",
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "123-456-7890",
                UserName = "john@example.com"
            };

            var tickets = new List<Ticket>
            {
                new Ticket
                {
                    Guid = Guid.NewGuid(),
                    Description = "Test Ticket",
                    TicketStatus = Status.Pending,
                    Customer = customer,
                    Comments = new List<string>()
                }
            };

            _mockTicketRepo
                .Setup(r => r.GetAllWithDetailsAsync())
                .ReturnsAsync(tickets);

            // Act
            var result = await _ticketService.GetAllTicketsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            var ticket = result.First();
            Assert.Equal("Test Ticket", ticket.Description);
        }

        [Fact]
        public async Task GetTicketByIdAsync_WithValidId_ReturnsTicket()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var customer = new Customer
            {
                Id = "cust-1",
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "123-456-7890",
                UserName = "john@example.com"
            };

            var ticket = new Ticket
            {
                Guid = ticketId,
                Description = "Test Ticket",
                TicketStatus = Status.Pending,
                Customer = customer,
                Comments = new List<string>()
            };

            _mockTicketRepo
                .Setup(r => r.GetByIdWithDetailsAsync(ticketId))
                .ReturnsAsync(ticket);

            // Act
            var result = await _ticketService.GetTicketByIdAsync(ticketId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Ticket", result.Description);
        }

        [Fact]
        public async Task AssignTicketAsync_AssignsUserAndSendsEmail()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var userId = "user-123";
            
            var customer = new Customer
            {
                Id = "cust-1",
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "123-456-7890",
                UserName = "john@example.com"
            };

            var ticket = new Ticket
            {
                Guid = ticketId,
                Description = "Test Ticket",
                TicketStatus = Status.Pending,
                Customer = customer,
                Comments = new List<string>()
            };

            var user = new Employee
            {
                Id = userId,
                FirstName = "Employee",
                LastName = "User",
                Email = "employee@example.com",
                Phone = "123-456-7890",
                UserName = "employee@example.com",
                Team = "Support",
                Level = EmployeeType.Support
            };

            _mockTicketRepo
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            _mockUserManager
                .Setup(um => um.FindByIdAsync(userId))
                .ReturnsAsync(user);

            _mockTicketRepo
                .Setup(r => r.UpdateAsync(It.IsAny<Ticket>()))
                .Returns(Task.CompletedTask);

            _mockTicketRepo
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            await _ticketService.AssignTicketAsync(ticketId, userId);

            // Assert
            Assert.Equal(user, ticket.Responsible);
            Assert.Equal(Status.Assigned, ticket.TicketStatus);
            _mockEmailService.Verify(e => e.SendTicketNotificationAsync(
                user.Email!, ticket.Description), Times.Once);
        }

        [Fact]
        public async Task UpdateTicketStatusAsync_UpdatesStatus()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var customer = new Customer
            {
                Id = "cust-1",
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "123-456-7890",
                UserName = "john@example.com"
            };

            var ticket = new Ticket
            {
                Guid = ticketId,
                Description = "Test Ticket",
                TicketStatus = Status.InProgress,
                Customer = customer,
                Comments = new List<string>()
            };

            _mockTicketRepo
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            _mockTicketRepo
                .Setup(r => r.UpdateAsync(It.IsAny<Ticket>()))
                .Returns(Task.CompletedTask);

            _mockTicketRepo
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            await _ticketService.UpdateTicketStatusAsync(ticketId, Status.Completed);

            // Assert
            Assert.Equal(Status.Completed, ticket.TicketStatus);
            Assert.NotNull(ticket.CompletionDate);
        }

        [Fact]
        public async Task AddCommentAsync_AddsCommentWithMetadata()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var userId = "user-123";
            var comment = "This is a test comment";
            
            var customer = new Customer
            {
                Id = "cust-1",
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "123-456-7890",
                UserName = "john@example.com"
            };

            var ticket = new Ticket
            {
                Guid = ticketId,
                Description = "Test Ticket",
                TicketStatus = Status.InProgress,
                Customer = customer,
                Comments = new List<string>()
            };

            var user = new Employee
            {
                Id = userId,
                FirstName = "Commenter",
                LastName = "User",
                Email = "commenter@example.com",
                Phone = "123-456-7890",
                UserName = "commenter@example.com",
                Team = "Support",
                Level = EmployeeType.Support
            };

            _mockTicketRepo
                .Setup(r => r.GetByIdAsync(ticketId))
                .ReturnsAsync(ticket);

            _mockUserManager
                .Setup(um => um.FindByIdAsync(userId))
                .ReturnsAsync(user);

            _mockTicketRepo
                .Setup(r => r.UpdateAsync(It.IsAny<Ticket>()))
                .Returns(Task.CompletedTask);

            _mockTicketRepo
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            await _ticketService.AddCommentAsync(ticketId, comment, userId);

            // Assert
            Assert.Single(ticket.Comments);
            Assert.Contains("Commenter User", ticket.Comments[0]);
            Assert.Contains(comment, ticket.Comments[0]);
        }

        [Fact]
        public async Task GetOverdueTicketsAsync_ReturnsOverdueTickets()
        {
            // Arrange
            var customer = new Customer
            {
                Id = "cust-1",
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "123-456-7890",
                UserName = "john@example.com"
            };

            var overdueTickets = new List<Ticket>
            {
                new Ticket
                {
                    Guid = Guid.NewGuid(),
                    Description = "Overdue Ticket",
                    TicketStatus = Status.InProgress,
                    CompletionTarget = DateTime.UtcNow.AddDays(-1),
                    Customer = customer,
                    Comments = new List<string>()
                }
            };

            _mockTicketRepo
                .Setup(r => r.GetOverdueTicketsAsync())
                .ReturnsAsync(overdueTickets);

            // Act
            var result = await _ticketService.GetOverdueTicketsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task AddWatcherAsync_AddsWatcherToTicket()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var userId = "user-123";
            
            var customer = new Customer
            {
                Id = "cust-1",
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "123-456-7890",
                UserName = "john@example.com"
            };

            var ticket = new Ticket
            {
                Guid = ticketId,
                Description = "Test Ticket",
                TicketStatus = Status.InProgress,
                Customer = customer,
                Comments = new List<string>(),
                Watchers = new List<ApplicationUser>()
            };

            var user = new Employee
            {
                Id = userId,
                FirstName = "Watcher",
                LastName = "User",
                Email = "watcher@example.com",
                Phone = "123-456-7890",
                UserName = "watcher@example.com",
                Team = "Support",
                Level = EmployeeType.Support
            };

            _mockTicketRepo
                .Setup(r => r.GetByIdWithDetailsAsync(ticketId))
                .ReturnsAsync(ticket);

            _mockUserManager
                .Setup(um => um.FindByIdAsync(userId))
                .ReturnsAsync(user);

            _mockTicketRepo
                .Setup(r => r.UpdateAsync(It.IsAny<Ticket>()))
                .Returns(Task.CompletedTask);

            _mockTicketRepo
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            await _ticketService.AddWatcherAsync(ticketId, userId);

            // Assert
            Assert.Contains(user, ticket.Watchers);
        }

        [Fact]
        public async Task RemoveWatcherAsync_RemovesWatcherFromTicket()
        {
            // Arrange
            var ticketId = Guid.NewGuid();
            var userId = "user-123";
            
            var customer = new Customer
            {
                Id = "cust-1",
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "123-456-7890",
                UserName = "john@example.com"
            };

            var user = new Employee
            {
                Id = userId,
                FirstName = "Watcher",
                LastName = "User",
                Email = "watcher@example.com",
                Phone = "123-456-7890",
                UserName = "watcher@example.com",
                Team = "Support",
                Level = EmployeeType.Support
            };

            var ticket = new Ticket
            {
                Guid = ticketId,
                Description = "Test Ticket",
                TicketStatus = Status.InProgress,
                Customer = customer,
                Comments = new List<string>(),
                Watchers = new List<ApplicationUser> { user }
            };

            _mockTicketRepo
                .Setup(r => r.GetByIdWithDetailsAsync(ticketId))
                .ReturnsAsync(ticket);

            _mockTicketRepo
                .Setup(r => r.UpdateAsync(It.IsAny<Ticket>()))
                .Returns(Task.CompletedTask);

            _mockTicketRepo
                .Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            await _ticketService.RemoveWatcherAsync(ticketId, userId);

            // Assert
            Assert.DoesNotContain(user, ticket.Watchers);
        }
    }
}
