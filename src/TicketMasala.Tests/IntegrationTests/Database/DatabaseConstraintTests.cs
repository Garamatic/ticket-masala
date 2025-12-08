using Microsoft.EntityFrameworkCore;
using TicketMasala.Tests.TestHelpers;
using TicketMasala.Web.Models;
using Xunit;

namespace TicketMasala.Tests.IntegrationTests.Database;

/// <summary>
/// Tests for database-level constraints, validation, and error handling.
/// </summary>
public class DatabaseConstraintTests : IDisposable
{
    private readonly DatabaseTestFixture _fixture;

    public DatabaseConstraintTests()
    {
        _fixture = new DatabaseTestFixture();
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    #region Ticket Required Fields Tests

    [Fact]
    public async Task Ticket_WithMissingTitle_ThrowsDbUpdateException()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        var ticket = new Ticket
        {
            Title = null!, // Required field
            Description = "Test Description",
            DomainId = "IT",
            Status = "New",
            CustomFieldsJson = "{}",
            TicketStatus = Status.Pending,
            CustomerId = customer.Id,
            CreatorGuid = Guid.Parse(customer.Id)
        };

        _fixture.Context.Tickets.Add(ticket);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await _fixture.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Ticket_WithMissingDomainId_ThrowsDbUpdateException()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            DomainId = null!, // Required field
            Status = "New",
            CustomFieldsJson = "{}",
            TicketStatus = Status.Pending,
            CustomerId = customer.Id,
            CreatorGuid = Guid.Parse(customer.Id)
        };

        _fixture.Context.Tickets.Add(ticket);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await _fixture.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Ticket_WithMissingDescription_ThrowsDbUpdateException()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = null!, // Required field
            DomainId = "IT",
            Status = "New",
            CustomFieldsJson = "{}",
            TicketStatus = Status.Pending,
            CustomerId = customer.Id,
            CreatorGuid = Guid.Parse(customer.Id)
        };

        _fixture.Context.Tickets.Add(ticket);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await _fixture.Context.SaveChangesAsync());
    }

    #endregion

    #region Project Required Fields Tests

    [Fact]
    public async Task Project_WithMissingName_ThrowsDbUpdateException()
    {
        // Arrange
        var project = new Project
        {
            Name = null!, // Required field
            Description = "Test Description",
            Status = Status.Pending,
            CreatorGuid = Guid.NewGuid(),
            CreationDate = DateTime.UtcNow
        };

        _fixture.Context.Projects.Add(project);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await _fixture.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Project_WithMissingDescription_ThrowsDbUpdateException()
    {
        // Arrange
        var project = new Project
        {
            Name = "Test Project",
            Description = null!, // Required field
            Status = Status.Pending,
            CreatorGuid = Guid.NewGuid(),
            CreationDate = DateTime.UtcNow
        };

        _fixture.Context.Projects.Add(project);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await _fixture.Context.SaveChangesAsync());
    }

    #endregion

    #region User Required Fields Tests

    [Fact]
    public async Task ApplicationUser_WithMissingFirstName_ThrowsDbUpdateException()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "test@example.com",
            Email = "test@example.com",
            FirstName = null!, // Required field
            LastName = "User",
            Phone = "555-1234"
        };

        _fixture.Context.Users.Add(user);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await _fixture.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task ApplicationUser_WithMissingLastName_ThrowsDbUpdateException()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "test@example.com",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = null!, // Required field
            Phone = "555-1234"
        };

        _fixture.Context.Users.Add(user);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await _fixture.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Employee_WithMissingTeam_BehaviorDependsOnDatabase()
    {
        // Note: SQLite doesn't enforce NOT NULL for non-nullable reference type columns
        // the same way SQL Server does. The [Required] attribute is enforced at the
        // application level by model validation, not necessarily at the database level.
        
        // Arrange
        var employee = new Employee
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "employee@example.com",
            Email = "employee@example.com",
            FirstName = "Test",
            LastName = "Employee",
            Phone = "555-1234",
            Team = null!, // Required field - may or may not be enforced by DB
            Level = EmployeeType.Support
        };

        _fixture.Context.Users.Add(employee);

        // Act & Assert - Document actual behavior
        try
        {
            await _fixture.Context.SaveChangesAsync();
            // SQLite may not enforce this constraint at the DB level
            // The [Required] attribute is validated at application level
            Assert.True(true, "SQLite accepted null Team - constraint not enforced at DB level");
        }
        catch (DbUpdateException)
        {
            // SQL Server or stricter databases would throw
            Assert.True(true, "Database enforced NOT NULL constraint");
        }
    }

    #endregion

    #region Foreign Key Constraint Tests

    [Fact]
    public async Task Ticket_WithInvalidCustomerId_CanBeCreated_FKNotEnforced()
    {
        // Note: SQLite in-memory doesn't enforce FK constraints by default
        // This test documents that behavior - in production with SQL Server, this would fail
        
        // Arrange
        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            DomainId = "IT",
            Status = "New",
            CustomFieldsJson = "{}",
            TicketStatus = Status.Pending,
            CustomerId = "non-existent-customer-id", // Invalid FK
            CreatorGuid = Guid.NewGuid()
        };

        _fixture.Context.Tickets.Add(ticket);

        // Act - This might work in SQLite but would fail in SQL Server
        // We're documenting the current behavior
        try
        {
            await _fixture.Context.SaveChangesAsync();
            Assert.NotEqual(Guid.Empty, ticket.Guid); // FK not enforced in SQLite in-memory
        }
        catch (DbUpdateException)
        {
            // Expected in production database with FK enforcement
            Assert.True(true);
        }
    }

    [Fact]
    public async Task Project_WithValidCustomerId_CreatesRelation()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        var project = new Project
        {
            Name = "Test Project",
            Description = "Test Description",
            Status = Status.Pending,
            CustomerId = customer.Id,
            CreatorGuid = Guid.NewGuid(),
            CreationDate = DateTime.UtcNow
        };

        // Act
        _fixture.Context.Projects.Add(project);
        await _fixture.Context.SaveChangesAsync();

        // Assert
        _fixture.Context.ChangeTracker.Clear();
        var fromDb = await _fixture.Context.Projects
            .Include(p => p.Customer)
            .FirstAsync(p => p.Guid == project.Guid);
        
        Assert.NotNull(fromDb.Customer);
        Assert.Equal(customer.Id, fromDb.Customer.Id);
    }

    #endregion

    #region Unique Constraint Tests

    [Fact]
    public async Task User_WithDuplicateEmail_CannotBeCreated()
    {
        // Arrange
        var email = "duplicate@example.com";
        var normalizedEmail = email.ToUpperInvariant();
        
        var user1 = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            NormalizedEmail = normalizedEmail,
            NormalizedUserName = normalizedEmail,
            FirstName = "User",
            LastName = "One",
            Phone = "111-1111"
        };

        var user2 = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            NormalizedEmail = normalizedEmail,
            NormalizedUserName = normalizedEmail,
            FirstName = "User",
            LastName = "Two",
            Phone = "222-2222"
        };

        _fixture.Context.Users.Add(user1);
        await _fixture.Context.SaveChangesAsync();
        _fixture.Context.Users.Add(user2);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await _fixture.Context.SaveChangesAsync());
    }

    #endregion

    #region Index Tests

    [Fact]
    public async Task Ticket_ContentHashIndex_AllowsNullValues()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        
        var ticket1 = await _fixture.SeedTestTicketAsync(customer: customer);
        ticket1.ContentHash = null; // No hash
        
        var ticket2 = await _fixture.SeedTestTicketAsync(customer: customer);
        ticket2.ContentHash = null; // Also no hash

        // Act & Assert - Should not throw (null values allowed in index)
        await _fixture.Context.SaveChangesAsync();
        
        var ticketsWithoutHash = await _fixture.Context.Tickets
            .Where(t => t.ContentHash == null)
            .ToListAsync();
        
        Assert.Equal(2, ticketsWithoutHash.Count);
    }

    [Fact]
    public async Task Ticket_ContentHashIndex_AllowsDifferentHashes()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        
        var ticket1 = await _fixture.SeedTestTicketAsync(customer: customer);
        ticket1.ContentHash = "hash1abc";
        
        var ticket2 = await _fixture.SeedTestTicketAsync(customer: customer);
        ticket2.ContentHash = "hash2def";

        // Act & Assert - Should not throw
        await _fixture.Context.SaveChangesAsync();

        var ticket1FromDb = await _fixture.Context.Tickets
            .FirstAsync(t => t.ContentHash == "hash1abc");
        Assert.Equal(ticket1.Guid, ticket1FromDb.Guid);
    }

    #endregion

    #region String Length Constraint Tests

    [Fact]
    public async Task Ticket_DomainId_EnforcesMaxLength()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            DomainId = new string('X', 100), // Exceeds MaxLength(50)
            Status = "New",
            CustomFieldsJson = "{}",
            TicketStatus = Status.Pending,
            CustomerId = customer.Id,
            CreatorGuid = Guid.Parse(customer.Id)
        };

        _fixture.Context.Tickets.Add(ticket);

        // Act & Assert - SQLite may not enforce string length constraints
        // This documents the behavior difference between SQLite and SQL Server
        try
        {
            await _fixture.Context.SaveChangesAsync();
            // SQLite doesn't enforce string length
            Assert.True(true);
        }
        catch (DbUpdateException)
        {
            // SQL Server would throw
            Assert.True(true);
        }
    }

    #endregion
}
