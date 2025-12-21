# Repositories & Gegevenstoegang

Documentatie voor het Repository-patroon en de laag voor gegevenstoegang in Ticket Masala.

## Overzicht

Ticket Masala implementeert het **Repository Pattern** met **Unit of Work** om de toegang tot gegevens te abstraheren van de bedrijfslogica.

```
Controller → Service → Repository → DbContext
                          ↓
                   Specificatiepatroon
```

**Voordelen:**
- Testbaarheid (mock repositories in tests).
- Database-onafhankelijk (vervang EF Core door Dapper, enz.).
- Gecentraliseerde query-logica.
- Consistente patronen voor gegevenstoegang.

---

## Repository-interfaces

### ITicketRepository

Primaire repository voor ticketbewerkingen.

```csharp
public interface ITicketRepository
{
    // === Leesbewerkingen ===
    Task<Ticket?> GetByIdAsync(Guid id, bool includeRelations = true);
    Task<IEnumerable<Ticket>> GetAllAsync(Guid? departmentId = null);
    Task<IEnumerable<Ticket>> GetUnassignedAsync(Guid? departmentId = null);
    Task<IEnumerable<Ticket>> GetByStatusAsync(Status status, Guid? departmentId = null);
    Task<IEnumerable<Ticket>> GetByCustomerIdAsync(string customerId);
    Task<IEnumerable<Ticket>> GetByResponsibleIdAsync(string responsibleId);
    Task<IEnumerable<Ticket>> GetByProjectGuidAsync(Guid projectGuid);
    Task<IEnumerable<Ticket>> GetRecentAsync(int timeWindowMinutes, Guid? departmentId = null);
    Task<TicketSearchViewModel> SearchTicketsAsync(TicketSearchViewModel searchModel, Guid? departmentId = null);

    // === Schrijfbewerkingen ===
    Task<Ticket> AddAsync(Ticket ticket);
    Task UpdateAsync(Ticket ticket);
    Task DeleteAsync(Guid id);

    // === Bulk-bewerkingen ===
    Task<IEnumerable<Ticket>> GetActiveTicketsAsync();
    Task<IEnumerable<Ticket>> GetCompletedTicketsAsync();
    Task<int> CountAsync();
    Task<bool> ExistsAsync(Guid id);

    // === Gerelateerde Gegevens ===
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

## Repository-implementatie

### ITicketRepository Voorbeeld

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
        _logger.LogInformation("Ticket {TicketGuid} aangemaakt", ticket.Guid);
        return ticket;
    }

    public async Task UpdateAsync(Ticket ticket)
    {
        ticket.ModifiedDate = DateTime.UtcNow;
        _context.Entry(ticket).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }
}
```

---

## Specificatiepatroon (Specification Pattern)

Gebruik specificaties voor complexe query's om de query-logica te kapselen.

### ISpecification-interface

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

---

## Unit of Work Patroon

Voor bewerkingen die meerdere repositories overspannen:

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

---

## DbContext

De `MasalaDbContext` biedt directe toegang tot EF Core:

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
}
```

---

## Beste Praktijken voor Queries

### Vermijd N+1 Queries

Gebruik 'Eager loading' met `.Include()` om te voorkomen dat er voor elk item in een lijst een aparte database-query wordt uitgevoerd.

### Gebruik Projecties

Haal alleen de benodigde velden op door de query te projecteren naar een DTO of ViewModel met `.Select()`.

### AsNoTracking voor alleen-lezen

Schakel 'change tracking' uit voor query's die alleen gegevens ophalen en niet bijwerken.

---

## Verdere Informatie

- [Domeinmodel](DOMAIN_MODEL.md) - Entiteitsdefinities
- [Testen Gids](../guides/TESTING.md) - Testpatronen
- [Architectuuroverzicht](SUMMARY.md) - Systeemontwerp
