# Repositories & Data Access

Documentation for the Repository pattern and data access layer in Ticket Masala.

## Overview

Ticket Masala implements the **Repository Pattern** with **Unit of Work** to abstract data access from business logic.

```
Controller → Service → Repository → DbContext
                          ↓
                   Specification Pattern
```

**Benefits:**
- Testability (mock repositories in tests)
- Database-agnostic (swap EF Core for Dapper, etc.)
- Centralized query logic
- Consistent data access patterns

---

## Repository Interfaces

### ITicketRepository

Primary repository for ticket operations.

```csharp
public interface ITicketRepository
{
    // === Read Operations ===
    Task<Ticket?> GetByIdAsync(Guid id, bool includeRelations = true);
    Task<IEnumerable<Ticket>> GetAllAsync(Guid? departmentId = null);
    Task<IEnumerable<Ticket>> GetUnassignedAsync(Guid? departmentId = null);
    Task<IEnumerable<Ticket>> GetByStatusAsync(Status status, Guid? departmentId = null);
    Task<IEnumerable<Ticket>> GetByCustomerIdAsync(string customerId);
    Task<IEnumerable<Ticket>> GetByResponsibleIdAsync(string responsibleId);
    Task<IEnumerable<Ticket>> GetByProjectGuidAsync(Guid projectGuid);
    Task<IEnumerable<Ticket>> GetRecentAsync(int timeWindowMinutes, Guid? departmentId = null);
    Task<TicketSearchViewModel> SearchTicketsAsync(TicketSearchViewModel searchModel, Guid? departmentId = null);

    // === Write Operations ===
    Task<Ticket> AddAsync(Ticket ticket);
    Task UpdateAsync(Ticket ticket);
    Task DeleteAsync(Guid id);

    // === Bulk Operations ===
    Task<IEnumerable<Ticket>> GetActiveTicketsAsync();
    Task<IEnumerable<Ticket>> GetCompletedTicketsAsync();
    Task<int> CountAsync();
    Task<bool> ExistsAsync(Guid id);

    // === Related Data ===
    Task<IEnumerable<Document>> GetDocumentsForTicketAsync(Guid ticketId);
    Task<IEnumerable<TicketComment>> GetCommentsForTicketAsync(Guid ticketId);
    Task<IEnumerable<QualityReview>> GetQualityReviewsForTicketAsync(Guid ticketId);
}
```

---

### IProjectRepository

```csharp
public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id);
    Task<IEnumerable<Project>> GetAllAsync();
    Task<IEnumerable<Project>> GetByCustomerIdAsync(string customerId);
    Task<IEnumerable<Project>> GetByManagerIdAsync(string managerId);
    Task<IEnumerable<Project>> SearchAsync(string query);
    
    Task<Project> AddAsync(Project project);
    Task UpdateAsync(Project project);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
```

---

### IUserRepository

```csharp
public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(string id);
    Task<ApplicationUser?> GetUserByEmailAsync(string email);
    Task<IEnumerable<ApplicationUser>> GetAllAsync();
    Task<IEnumerable<Employee>> GetEmployeesAsync();
    Task<IEnumerable<Employee>> GetAvailableAgentsAsync();
    
    Task UpdateAsync(ApplicationUser user);
}
```

---

## Repository Implementation

### TicketRepository Example

```csharp
public class TicketRepository : ITicketRepository
{
    private readonly MasalaDbContext _context;
    private readonly ILogger<TicketRepository> _logger;

    public TicketRepository(MasalaDbContext context, ILogger<TicketRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Ticket?> GetByIdAsync(Guid id, bool includeRelations = true)
    {
        var query = _context.Tickets.AsQueryable();

        if (includeRelations)
        {
            query = query
                .Include(t => t.Customer)
                .Include(t => t.Responsible)
                .Include(t => t.Project)
                .Include(t => t.Comments);
        }

        return await query.FirstOrDefaultAsync(t => t.Guid == id);
    }

    public async Task<IEnumerable<Ticket>> GetByStatusAsync(Status status, Guid? departmentId = null)
    {
        return await _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Responsible)
            .Where(t => t.TicketStatus == status)
            .OrderByDescending(t => t.PriorityScore)
            .ToListAsync();
    }

    public async Task<Ticket> AddAsync(Ticket ticket)
    {
        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created ticket {TicketGuid}", ticket.Guid);
        return ticket;
    }

    public async Task UpdateAsync(Ticket ticket)
    {
        ticket.ModifiedDate = DateTime.UtcNow;
        _context.Entry(ticket).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket != null)
        {
            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();
        }
    }
}
```

---

## Specification Pattern

For complex queries, use specifications to encapsulate query logic.

### ISpecification Interface

```csharp
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }
    int? Take { get; }
    int? Skip { get; }
}
```

### Specification Example

```csharp
public class OpenTicketsForAgentSpec : BaseSpecification<Ticket>
{
    public OpenTicketsForAgentSpec(string agentId)
        : base(t => t.ResponsibleId == agentId 
                 && t.TicketStatus != Status.Completed 
                 && t.TicketStatus != Status.Cancelled)
    {
        AddInclude(t => t.Customer);
        AddInclude(t => t.Project);
        AddOrderByDescending(t => t.PriorityScore);
    }
}

// Usage
var spec = new OpenTicketsForAgentSpec(agentId);
var tickets = await _repository.GetAsync(spec);
```

---

## Unit of Work Pattern

For operations spanning multiple repositories:

```csharp
public interface IUnitOfWork : IDisposable
{
    ITicketRepository Tickets { get; }
    IProjectRepository Projects { get; }
    IUserRepository Users { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
```

### Usage

```csharp
public class ProjectService
{
    private readonly IUnitOfWork _unitOfWork;

    public async Task CreateProjectWithTicketsAsync(Project project, List<Ticket> tickets)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            await _unitOfWork.Projects.AddAsync(project);
            
            foreach (var ticket in tickets)
            {
                ticket.ProjectGuid = project.Guid;
                await _unitOfWork.Tickets.AddAsync(ticket);
            }
            
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
```

---

## Registration

Repositories are registered in `Extensions/RepositoryExtensions.cs`:

```csharp
public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        
        // Optional: Register Unit of Work
        // services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        return services;
    }
}
```

---

## DbContext

The `MasalaDbContext` provides direct EF Core access:

```csharp
public class MasalaDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<TicketComment> Comments => Set<TicketComment>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DomainConfigVersion> DomainConfigVersions => Set<DomainConfigVersion>();
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Configure entity relationships
        builder.ApplyConfigurationsFromAssembly(typeof(MasalaDbContext).Assembly);
    }
}
```

---

## Query Best Practices

### Avoid N+1 Queries

```csharp
// Bad - N+1 problem
var tickets = await _context.Tickets.ToListAsync();
foreach (var ticket in tickets)
{
    var customer = ticket.Customer; // Lazy load for each ticket
}

// Good - Eager loading
var tickets = await _context.Tickets
    .Include(t => t.Customer)
    .ToListAsync();
```

### Use Projections

```csharp
// Bad - Fetches entire entity
var tickets = await _context.Tickets.ToListAsync();

// Good - Project to DTO/ViewModel
var ticketSummaries = await _context.Tickets
    .Select(t => new TicketSummaryDto
    {
        Id = t.Guid,
        Title = t.Title,
        Status = t.Status,
        CustomerName = t.Customer.FullName
    })
    .ToListAsync();
```

### AsNoTracking for Read-Only

```csharp
// For read-only queries, disable change tracking
var tickets = await _context.Tickets
    .AsNoTracking()
    .Where(t => t.TicketStatus == Status.Pending)
    .ToListAsync();
```

---

## Testing Repositories

```csharp
public class TicketRepositoryTests
{
    private readonly MasalaDbContext _context;
    private readonly TicketRepository _sut;

    public TicketRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new MasalaDbContext(options);
        _sut = new TicketRepository(_context, Mock.Of<ILogger<TicketRepository>>());
    }

    [Fact]
    public async Task AddAsync_ValidTicket_ReturnsTicket()
    {
        // Arrange
        var ticket = TestDataFactory.CreateTicket();

        // Act
        var result = await _sut.AddAsync(ticket);

        // Assert
        result.Should().NotBeNull();
        result.Guid.Should().Be(ticket.Guid);
        (await _context.Tickets.CountAsync()).Should().Be(1);
    }
}
```

---

## Further Reading

- [Domain Model](DOMAIN_MODEL.md) - Entity definitions
- [Testing Guide](../guides/TESTING.md) - Testing patterns
- [Architecture Overview](SUMMARY.md) - System design
