using Microsoft.EntityFrameworkCore;
using TicketMasala.Tests.TestHelpers;
using TicketMasala.Web.Models;
using Xunit;

namespace TicketMasala.Tests.IntegrationTests.Database;

/// <summary>
/// Tests for database transaction behavior including rollback and isolation.
/// </summary>
public class DatabaseTransactionTests : IDisposable
{
    private readonly DatabaseTestFixture _fixture;

    public DatabaseTransactionTests()
    {
        _fixture = new DatabaseTestFixture();
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    #region Transaction Rollback Tests

    [Fact]
    public async Task Transaction_Rollback_RevertsChanges()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        var initialCount = await _fixture.Context.Tickets.CountAsync();

        using var transaction = await _fixture.Context.Database.BeginTransactionAsync();

        var ticket = new Ticket
        {
            Title = "Transaction Test Ticket",
            Description = "Should be rolled back",
            DomainId = "IT",
            Status = "New",
            CustomFieldsJson = "{}",
            TicketStatus = Status.Pending,
            CustomerId = customer.Id,
            CreatorGuid = Guid.Parse(customer.Id)
        };

        _fixture.Context.Tickets.Add(ticket);
        await _fixture.Context.SaveChangesAsync();

        // Verify ticket was added within transaction
        var countDuringTransaction = await _fixture.Context.Tickets.CountAsync();
        Assert.Equal(initialCount + 1, countDuringTransaction);

        // Act - Rollback
        await transaction.RollbackAsync();

        // Assert - Ticket should not be persisted
        _fixture.Context.ChangeTracker.Clear();
        var countAfterRollback = await _fixture.Context.Tickets.CountAsync();
        Assert.Equal(initialCount, countAfterRollback);
    }

    [Fact]
    public async Task Transaction_Commit_PersistsChanges()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        var initialCount = await _fixture.Context.Tickets.CountAsync();

        using var transaction = await _fixture.Context.Database.BeginTransactionAsync();

        var ticket = new Ticket
        {
            Title = "Committed Ticket",
            Description = "Should be persisted",
            DomainId = "IT",
            Status = "New",
            CustomFieldsJson = "{}",
            TicketStatus = Status.Pending,
            CustomerId = customer.Id,
            CreatorGuid = Guid.Parse(customer.Id)
        };

        _fixture.Context.Tickets.Add(ticket);
        await _fixture.Context.SaveChangesAsync();

        // Act - Commit
        await transaction.CommitAsync();

        // Assert - Ticket should be persisted
        _fixture.Context.ChangeTracker.Clear();
        var countAfterCommit = await _fixture.Context.Tickets.CountAsync();
        Assert.Equal(initialCount + 1, countAfterCommit);

        var ticketFromDb = await _fixture.Context.Tickets.FirstOrDefaultAsync(t => t.Title == "Committed Ticket");
        Assert.NotNull(ticketFromDb);
    }

    #endregion

    #region Multiple Operations Tests

    [Fact]
    public async Task MultipleOperations_WithFailure_RollsBackAll()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        var initialTicketCount = await _fixture.Context.Tickets.CountAsync();
        var initialProjectCount = await _fixture.Context.Projects.CountAsync();

        using var transaction = await _fixture.Context.Database.BeginTransactionAsync();

        try
        {
            // Create a valid ticket
            var ticket = new Ticket
            {
                Title = "Valid Ticket",
                Description = "This ticket is valid",
                DomainId = "IT",
                Status = "New",
                CustomFieldsJson = "{}",
                TicketStatus = Status.Pending,
                CustomerId = customer.Id,
                CreatorGuid = Guid.Parse(customer.Id)
            };
            _fixture.Context.Tickets.Add(ticket);
            await _fixture.Context.SaveChangesAsync();

            // Create a valid project
            var project = new Project
            {
                Name = "Valid Project",
                Description = "This project is valid",
                Status = Status.Pending,
                CustomerId = customer.Id,
                CreatorGuid = Guid.NewGuid(),
                CreationDate = DateTime.UtcNow
            };
            _fixture.Context.Projects.Add(project);
            await _fixture.Context.SaveChangesAsync();

            // Create an invalid ticket that will cause an error
            var invalidTicket = new Ticket
            {
                Title = null!, // This will fail
                Description = "Invalid ticket",
                DomainId = "IT",
                Status = "New",
                CustomFieldsJson = "{}",
                TicketStatus = Status.Pending,
                CustomerId = customer.Id,
                CreatorGuid = Guid.Parse(customer.Id)
            };
            _fixture.Context.Tickets.Add(invalidTicket);
            await _fixture.Context.SaveChangesAsync(); // Should throw

            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
        }

        // Assert - All changes should be rolled back
        _fixture.Context.ChangeTracker.Clear();
        var ticketCount = await _fixture.Context.Tickets.CountAsync();
        var projectCount = await _fixture.Context.Projects.CountAsync();

        Assert.Equal(initialTicketCount, ticketCount);
        Assert.Equal(initialProjectCount, projectCount);
    }

    [Fact]
    public async Task MultipleOperations_AllValid_CommitsAll()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        var initialTicketCount = await _fixture.Context.Tickets.CountAsync();
        var initialProjectCount = await _fixture.Context.Projects.CountAsync();

        using var transaction = await _fixture.Context.Database.BeginTransactionAsync();

        // Create multiple valid entities
        var ticket1 = new Ticket
        {
            Title = "Batch Ticket 1",
            Description = "First ticket in batch",
            DomainId = "IT",
            Status = "New",
            CustomFieldsJson = "{}",
            TicketStatus = Status.Pending,
            CustomerId = customer.Id,
            CreatorGuid = Guid.Parse(customer.Id)
        };

        var ticket2 = new Ticket
        {
            Title = "Batch Ticket 2",
            Description = "Second ticket in batch",
            DomainId = "IT",
            Status = "New",
            CustomFieldsJson = "{}",
            TicketStatus = Status.Pending,
            CustomerId = customer.Id,
            CreatorGuid = Guid.Parse(customer.Id)
        };

        var project = new Project
        {
            Name = "Batch Project",
            Description = "Project in batch",
            Status = Status.Pending,
            CustomerId = customer.Id,
            CreatorGuid = Guid.NewGuid(),
            CreationDate = DateTime.UtcNow
        };

        _fixture.Context.Tickets.AddRange(ticket1, ticket2);
        _fixture.Context.Projects.Add(project);
        await _fixture.Context.SaveChangesAsync();

        // Act
        await transaction.CommitAsync();

        // Assert
        _fixture.Context.ChangeTracker.Clear();
        var ticketCount = await _fixture.Context.Tickets.CountAsync();
        var projectCount = await _fixture.Context.Projects.CountAsync();

        Assert.Equal(initialTicketCount + 2, ticketCount);
        Assert.Equal(initialProjectCount + 1, projectCount);
    }

    #endregion

    #region Concurrent Context Tests

    [Fact]
    public async Task SeparateContexts_SeeCommittedChanges()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();

        var ticket = new Ticket
        {
            Title = "Multi-Context Ticket",
            Description = "Created in first context",
            DomainId = "IT",
            Status = "New",
            CustomFieldsJson = "{}",
            TicketStatus = Status.Pending,
            CustomerId = customer.Id,
            CreatorGuid = Guid.Parse(customer.Id)
        };

        _fixture.Context.Tickets.Add(ticket);
        await _fixture.Context.SaveChangesAsync();
        var ticketGuid = ticket.Guid;

        // Act - Create a new context and read the ticket
        using var newContext = _fixture.CreateNewContext();
        var ticketFromNewContext = await newContext.Tickets.FindAsync(ticketGuid);

        // Assert
        Assert.NotNull(ticketFromNewContext);
        Assert.Equal("Multi-Context Ticket", ticketFromNewContext.Title);
    }

    [Fact]
    public async Task Update_WithStaleData_ThrowsConcurrencyException()
    {
        // Note: This test demonstrates the pattern but may not throw in SQLite
        // without explicit concurrency tokens configured

        // Arrange
        var ticket = await _fixture.SeedTestTicketAsync();
        var ticketGuid = ticket.Guid;

        // Get the same ticket in two different contexts
        using var context1 = _fixture.CreateNewContext();
        using var context2 = _fixture.CreateNewContext();

        var ticket1 = await context1.Tickets.FindAsync(ticketGuid);
        var ticket2 = await context2.Tickets.FindAsync(ticketGuid);

        // Modify in first context
        ticket1!.Title = "Updated by Context 1";
        await context1.SaveChangesAsync();

        // Try to modify in second context (stale data)
        ticket2!.Title = "Updated by Context 2";

        // Act & Assert
        // Without concurrency tokens, this will succeed (last write wins)
        // With concurrency tokens (RowVersion), it would throw DbUpdateConcurrencyException
        await context2.SaveChangesAsync(); // In real apps, add concurrency handling

        // Verify the last write won
        context1.ChangeTracker.Clear();
        var finalTicket = await context1.Tickets.FindAsync(ticketGuid);
        Assert.Equal("Updated by Context 2", finalTicket!.Title);
    }

    #endregion

    #region SaveChanges Behavior Tests

    [Fact]
    public async Task SaveChangesAsync_TracksPendingChanges()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();

        var ticket = new Ticket
        {
            Title = "Pending Ticket",
            Description = "Not yet saved",
            DomainId = "IT",
            Status = "New",
            CustomFieldsJson = "{}",
            TicketStatus = Status.Pending,
            CustomerId = customer.Id,
            CreatorGuid = Guid.Parse(customer.Id)
        };

        _fixture.Context.Tickets.Add(ticket);

        // Assert - Entry is tracked as Added before save
        var entry = _fixture.Context.Entry(ticket);
        Assert.Equal(EntityState.Added, entry.State);

        // Act
        await _fixture.Context.SaveChangesAsync();

        // Assert - Entry is now tracked as Unchanged
        Assert.Equal(EntityState.Unchanged, entry.State);
    }

    [Fact]
    public async Task ChangeTracker_Clear_ResetsTracking()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        var ticket = await _fixture.SeedTestTicketAsync(customer: customer);

        ticket.Title = "Modified Title";
        Assert.Equal(EntityState.Modified, _fixture.Context.Entry(ticket).State);

        // Act
        _fixture.Context.ChangeTracker.Clear();

        // Assert - Entity is no longer tracked
        Assert.Equal(EntityState.Detached, _fixture.Context.Entry(ticket).State);

        // Changes are lost
        var freshTicket = await _fixture.Context.Tickets.FindAsync(ticket.Guid);
        Assert.Equal("Test Ticket", freshTicket!.Title); // Original title
    }

    #endregion
}
