using Microsoft.EntityFrameworkCore;
using TicketMasala.Tests.TestHelpers;
using TicketMasala.Web.Models;
using Xunit;

namespace TicketMasala.Tests.IntegrationTests.Database;

/// <summary>
/// Integration tests for EfCoreTicketRepository using real SQLite database.
/// </summary>
public class EfCoreTicketRepositoryIntegrationTests : IDisposable
{
    private readonly DatabaseTestFixture _fixture;

    public EfCoreTicketRepositoryIntegrationTests()
    {
        _fixture = new DatabaseTestFixture();
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidTicket_CreatesTicketInDatabase()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        var ticket = new Ticket
        {
            Title = "New Test Ticket",
            Description = "Test Description",
            DomainId = "IT",
            Status = "New",
            CustomFieldsJson = "{}",
            TicketStatus = Status.Pending,
            CustomerId = customer.Id,
            CreatorGuid = Guid.Parse(customer.Id)
        };

        // Act
        var result = await _fixture.TicketRepository.AddAsync(ticket);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Guid);
        
        // Verify it's actually in the database
        var fromDb = await _fixture.Context.Tickets.FindAsync(result.Guid);
        Assert.NotNull(fromDb);
        Assert.Equal("New Test Ticket", fromDb.Title);
    }

    [Fact]
    public async Task AddAsync_WithGerdaFields_PersistsAIData()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        var ticket = new Ticket
        {
            Title = "AI Ticket",
            Description = "Test Description",
            DomainId = "IT",
            Status = "New",
            CustomFieldsJson = "{}",
            TicketStatus = Status.Pending,
            CustomerId = customer.Id,
            CreatorGuid = Guid.Parse(customer.Id),
            EstimatedEffortPoints = 8,
            PriorityScore = 75.5,
            GerdaTags = "AI-Dispatched,High-Priority"
        };

        // Act
        await _fixture.TicketRepository.AddAsync(ticket);

        // Assert
        var fromDb = await _fixture.Context.Tickets.FindAsync(ticket.Guid);
        Assert.NotNull(fromDb);
        Assert.Equal(8, fromDb.EstimatedEffortPoints);
        Assert.Equal(75.5, fromDb.PriorityScore);
        Assert.Equal("AI-Dispatched,High-Priority", fromDb.GerdaTags);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsTicket()
    {
        // Arrange
        var ticket = await _fixture.SeedTestTicketAsync();

        // Act
        var result = await _fixture.TicketRepository.GetByIdAsync(ticket.Guid);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ticket.Guid, result.Guid);
        Assert.Equal("Test Ticket", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WithIncludeRelations_LoadsCustomer()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        var ticket = await _fixture.SeedTestTicketAsync(customer: customer);

        // Clear tracking to force reload
        _fixture.Context.ChangeTracker.Clear();

        // Act
        var result = await _fixture.TicketRepository.GetByIdAsync(ticket.Guid, includeRelations: true);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Customer);
        Assert.Equal(customer.Id, result.Customer.Id);
        Assert.Equal("Test Customer", result.Customer.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _fixture.TicketRepository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ModifiesExistingTicket()
    {
        // Arrange
        var ticket = await _fixture.SeedTestTicketAsync();
        ticket.Title = "Updated Title";
        ticket.TicketStatus = Status.InProgress;

        // Act
        await _fixture.TicketRepository.UpdateAsync(ticket);

        // Assert
        var fromDb = await _fixture.Context.Tickets.FindAsync(ticket.Guid);
        Assert.NotNull(fromDb);
        Assert.Equal("Updated Title", fromDb.Title);
        Assert.Equal(Status.InProgress, fromDb.TicketStatus);
    }

    [Fact]
    public async Task UpdateAsync_AssignsResponsible_UpdatesStatus()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        var employee = await _fixture.SeedTestEmployeeAsync();
        var ticket = await _fixture.SeedTestTicketAsync(customer: customer);
        
        ticket.ResponsibleId = employee.Id;
        ticket.TicketStatus = Status.Assigned;

        // Act
        await _fixture.TicketRepository.UpdateAsync(ticket);

        // Assert
        _fixture.Context.ChangeTracker.Clear();
        var fromDb = await _fixture.Context.Tickets
            .Include(t => t.Responsible)
            .FirstAsync(t => t.Guid == ticket.Guid);
        
        Assert.Equal(employee.Id, fromDb.ResponsibleId);
        Assert.Equal(Status.Assigned, fromDb.TicketStatus);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_RemovesTicketFromDatabase()
    {
        // Arrange
        var ticket = await _fixture.SeedTestTicketAsync();
        var ticketGuid = ticket.Guid;

        // Act
        await _fixture.TicketRepository.DeleteAsync(ticketGuid);

        // Assert
        var fromDb = await _fixture.Context.Tickets.FindAsync(ticketGuid);
        Assert.Null(fromDb);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_DoesNotThrow()
    {
        // Act & Assert - should not throw
        await _fixture.TicketRepository.DeleteAsync(Guid.NewGuid());
    }

    #endregion

    #region GetByStatusAsync Tests

    [Fact]
    public async Task GetByStatusAsync_FiltersCorrectly()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        await _fixture.SeedTestTicketAsync(customer: customer, status: Status.Pending);
        await _fixture.SeedTestTicketAsync(customer: customer, status: Status.Pending);
        await _fixture.SeedTestTicketAsync(customer: customer, status: Status.Completed);

        // Act
        var pendingTickets = await _fixture.TicketRepository.GetByStatusAsync(Status.Pending);
        var completedTickets = await _fixture.TicketRepository.GetByStatusAsync(Status.Completed);

        // Assert
        Assert.Equal(2, pendingTickets.Count());
        Assert.Single(completedTickets);
    }

    #endregion

    #region GetByCustomerIdAsync Tests

    [Fact]
    public async Task GetByCustomerIdAsync_ReturnsOnlyCustomerTickets()
    {
        // Arrange
        var customer1 = await _fixture.SeedTestCustomerAsync();
        var customer2 = await _fixture.SeedTestCustomerAsync();
        
        await _fixture.SeedTestTicketAsync(customer: customer1);
        await _fixture.SeedTestTicketAsync(customer: customer1);
        await _fixture.SeedTestTicketAsync(customer: customer2);

        // Act
        var tickets = await _fixture.TicketRepository.GetByCustomerIdAsync(customer1.Id);

        // Assert
        Assert.Equal(2, tickets.Count());
        Assert.All(tickets, t => Assert.Equal(customer1.Id, t.CustomerId));
    }

    #endregion

    #region GetByResponsibleIdAsync Tests

    [Fact]
    public async Task GetByResponsibleIdAsync_ReturnsAssignedTickets()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        var employee1 = await _fixture.SeedTestEmployeeAsync();
        var employee2 = await _fixture.SeedTestEmployeeAsync();
        
        await _fixture.SeedTestTicketAsync(customer: customer, responsible: employee1);
        await _fixture.SeedTestTicketAsync(customer: customer, responsible: employee1);
        await _fixture.SeedTestTicketAsync(customer: customer, responsible: employee2);
        await _fixture.SeedTestTicketAsync(customer: customer); // Unassigned

        // Act
        var tickets = await _fixture.TicketRepository.GetByResponsibleIdAsync(employee1.Id);

        // Assert
        Assert.Equal(2, tickets.Count());
        Assert.All(tickets, t => Assert.Equal(employee1.Id, t.ResponsibleId));
    }

    #endregion

    #region GetActiveTicketsAsync Tests

    [Fact]
    public async Task GetActiveTicketsAsync_ExcludesCompletedAndFailed()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        await _fixture.SeedTestTicketAsync(customer: customer, status: Status.Pending);
        await _fixture.SeedTestTicketAsync(customer: customer, status: Status.InProgress);
        await _fixture.SeedTestTicketAsync(customer: customer, status: Status.Assigned);
        await _fixture.SeedTestTicketAsync(customer: customer, status: Status.Completed);
        await _fixture.SeedTestTicketAsync(customer: customer, status: Status.Failed);

        // Act
        var activeTickets = await _fixture.TicketRepository.GetActiveTicketsAsync();

        // Assert
        Assert.Equal(3, activeTickets.Count());
        Assert.All(activeTickets, t => 
            Assert.True(t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed));
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        await _fixture.SeedTestTicketAsync(customer: customer);
        await _fixture.SeedTestTicketAsync(customer: customer);
        await _fixture.SeedTestTicketAsync(customer: customer);

        // Act
        var count = await _fixture.TicketRepository.CountAsync();

        // Assert
        Assert.Equal(3, count);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingTicket_ReturnsTrue()
    {
        // Arrange
        var ticket = await _fixture.SeedTestTicketAsync();

        // Act
        var exists = await _fixture.TicketRepository.ExistsAsync(ticket.Guid);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingTicket_ReturnsFalse()
    {
        // Act
        var exists = await _fixture.TicketRepository.ExistsAsync(Guid.NewGuid());

        // Assert
        Assert.False(exists);
    }

    #endregion
}
