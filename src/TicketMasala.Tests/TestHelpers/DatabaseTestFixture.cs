using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TicketMasala.Web.Data;
using TicketMasala.Web.Models;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Tests.TestHelpers;

/// <summary>
/// Test fixture that provides a real SQLite database for integration testing.
/// Uses SQLite in-memory mode with a shared connection to persist data across the test.
/// </summary>
public class DatabaseTestFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    public MasalaDbContext Context { get; private set; }

    // Repositories
    public EfCoreTicketRepository TicketRepository { get; private set; }
    public EfCoreProjectRepository ProjectRepository { get; private set; }
    public EfCoreUserRepository UserRepository { get; private set; }

    public DatabaseTestFixture()
    {
        // Create and open a connection. This creates the SQLite in-memory database.
        // The database exists as long as the connection is open.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseSqlite(_connection)
            .EnableSensitiveDataLogging()
            .Options;

        Context = new MasalaDbContext(options);

        // Create the schema
        Context.Database.EnsureCreated();

        // Initialize repositories with mock loggers
        TicketRepository = new EfCoreTicketRepository(
            Context,
            Mock.Of<ILogger<EfCoreTicketRepository>>());

        ProjectRepository = new EfCoreProjectRepository(
            Context,
            Mock.Of<ILogger<EfCoreProjectRepository>>());

        UserRepository = new EfCoreUserRepository(
            Context,
            Mock.Of<ILogger<EfCoreUserRepository>>());
    }

    /// <summary>
    /// Creates a fresh context for transaction isolation testing.
    /// </summary>
    public MasalaDbContext CreateNewContext()
    {
        var options = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseSqlite(_connection)
            .EnableSensitiveDataLogging()
            .Options;

        return new MasalaDbContext(options);
    }

    /// <summary>
    /// Seeds the database with a standard test customer.
    /// </summary>
    public async Task<ApplicationUser> SeedTestCustomerAsync(string? id = null, string? email = null)
    {
        var customer = new ApplicationUser
        {
            Id = id ?? Guid.NewGuid().ToString(),
            UserName = email ?? $"customer_{Guid.NewGuid():N}@test.com",
            Email = email ?? $"customer_{Guid.NewGuid():N}@test.com",
            FirstName = "Test",
            LastName = "Customer",
            Phone = "555-1234",
            NormalizedEmail = (email ?? $"customer_{Guid.NewGuid():N}@test.com").ToUpperInvariant(),
            NormalizedUserName = (email ?? $"customer_{Guid.NewGuid():N}@test.com").ToUpperInvariant(),
            EmailConfirmed = true
        };

        Context.Users.Add(customer);
        await Context.SaveChangesAsync();
        return customer;
    }

    /// <summary>
    /// Seeds the database with a standard test employee.
    /// </summary>
    public async Task<Employee> SeedTestEmployeeAsync(
        string? id = null,
        string? email = null,
        EmployeeType level = EmployeeType.Support,
        string team = "Support")
    {
        var employee = new Employee
        {
            Id = id ?? Guid.NewGuid().ToString(),
            UserName = email ?? $"employee_{Guid.NewGuid():N}@test.com",
            Email = email ?? $"employee_{Guid.NewGuid():N}@test.com",
            FirstName = "Test",
            LastName = "Employee",
            Phone = "555-5678",
            Team = team,
            Level = level,
            Language = "EN",
            MaxCapacityPoints = 40,
            NormalizedEmail = (email ?? $"employee_{Guid.NewGuid():N}@test.com").ToUpperInvariant(),
            NormalizedUserName = (email ?? $"employee_{Guid.NewGuid():N}@test.com").ToUpperInvariant(),
            EmailConfirmed = true
        };

        Context.Users.Add(employee);
        await Context.SaveChangesAsync();
        return employee;
    }

    /// <summary>
    /// Seeds the database with a standard test ticket.
    /// </summary>
    public async Task<Ticket> SeedTestTicketAsync(
        ApplicationUser? customer = null,
        Employee? responsible = null,
        Status status = Status.Pending,
        string title = "Test Ticket",
        string description = "Test Description")
    {
        customer ??= await SeedTestCustomerAsync();

        var ticket = new Ticket
        {
            Guid = Guid.NewGuid(),
            Title = title,
            Description = description,
            DomainId = "IT",
            Status = "New",
            CustomFieldsJson = "{}",
            TicketStatus = status,
            CustomerId = customer.Id,
            Customer = customer,
            ResponsibleId = responsible?.Id,
            Responsible = responsible,
            CreatorGuid = Guid.Parse(customer.Id),
            CreationDate = DateTime.UtcNow,
            EstimatedEffortPoints = 5,
            PriorityScore = 50.0
        };

        Context.Tickets.Add(ticket);
        await Context.SaveChangesAsync();
        return ticket;
    }

    /// <summary>
    /// Seeds the database with a standard test project.
    /// </summary>
    public async Task<Project> SeedTestProjectAsync(
        ApplicationUser? customer = null,
        Employee? projectManager = null,
        Status status = Status.InProgress)
    {
        customer ??= await SeedTestCustomerAsync();

        var project = new Project
        {
            Guid = Guid.NewGuid(),
            Name = "Test Project",
            Description = "Test Project Description",
            Status = status,
            CustomerId = customer.Id,
            Customer = customer,
            ProjectManagerId = projectManager?.Id,
            ProjectManager = projectManager,
            CreatorGuid = Guid.NewGuid(),
            CreationDate = DateTime.UtcNow,
            CompletionTarget = DateTime.UtcNow.AddMonths(3)
        };

        Context.Projects.Add(project);
        await Context.SaveChangesAsync();
        return project;
    }

    /// <summary>
    /// Clears all data from the database.
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        Context.Tickets.RemoveRange(Context.Tickets);
        Context.Projects.RemoveRange(Context.Projects);
        Context.Users.RemoveRange(Context.Users);
        await Context.SaveChangesAsync();
    }

    public void Dispose()
    {
        Context?.Dispose();
        _connection?.Dispose();
    }
}

/// <summary>
/// Collection definition for database tests that share a fixture.
/// Use [Collection("Database")] on test classes that need database access.
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseTestFixture>
{
}
