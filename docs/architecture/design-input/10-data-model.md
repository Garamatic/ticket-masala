Excellent. We start at the metal. If the storage layer is naive, the entire engine rots.

Here is the Reference Implementation for the Data Layer.
The Architectural Goals

    Hybrid Schema: We treat Ticket as a "Universal Container." It has rigid SQL columns for the things that always exist (ID, Status, CreatedAt) and a flexible JSON column for the things that might exist (Department, SoilPH, TaxCode).

    Zero-Scan Filtering: We never run WHERE json_extract(body, '$.priority') > 10. That forces a full table scan. Instead, we materialize that path into a Generated Column and put a B-Tree index on it.

    Concurrency: We rely on optimistic concurrency (Version) to detect race conditions without locking the whole database.

1. The Universal Base & Entity Model

We don't just dump classes in a folder. We structure them for the engine.
C#

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketMasala.Data.Entities;

// The base for all persistent objects.
// Enforces standard ID generation and audit trails.
public abstract class MasalaEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString(); // SQLite works better with Strings or Int64. GUIDs as text are acceptable here for portability.

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Optimistic Concurrency Token
    [ConcurrencyCheck]
    public string Version { get; set; } = Guid.NewGuid().ToString();
}

public class Ticket : MasalaEntity
{
    // --- RIGID COLUMNS (The "Universal" Model) ---
    // These are first-class SQL columns because we ALWAYS sort/filter by them.

    [Required]
    [MaxLength(50)]
    public required string DomainId { get; set; } // e.g., "IT", "LEGAL". Partition key logic lives here.

    [Required]
    [MaxLength(20)]
    public required string Status { get; set; } // "New", "Triaged", "Done"

    public required string Title { get; set; }

    // Foreign Key to the specific Ruleset Snapshot used to create this ticket.
    // This ensures that if we change rules later, old tickets don't break.
    public required string ConfigVersionId { get; set; }

    // --- FLEXIBLE STORAGE (The "Masala" Model) ---
    
    // The raw dump of all domain-specific fields.
    // SQLite treats this as TEXT.
    [Column(TypeName = "TEXT")] 
    public string CustomFieldsJson { get; set; } = "{}";

    // --- GENERATED COLUMNS (The Performance Secret) ---
    // These properties do not exist in C# memory as settable values.
    // They are projected by SQLite from the JSON blob.

    public double? ComputedPriority { get; private set; } // Indexable Priority
    public string? ComputedCategory { get; private set; } // Indexable Category
}

2. The Context Configuration (The "Strict" Doctrine)

This is where we enforce the architectural constraints. We use OnModelCreating to tell SQLite exactly how to handle that JSON.
C#

using Microsoft.EntityFrameworkCore;
using TicketMasala.Data.Entities;

namespace TicketMasala.Data;

public class TicketDbContext : DbContext
{
    public DbSet<Ticket> Tickets { get; set; }

    public TicketDbContext(DbContextOptions<TicketDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Ticket>(entity =>
        {
            // 1. Performance: Index the DomainId for fast partitioning lookups
            entity.HasIndex(e => e.DomainId);
            entity.HasIndex(e => e.Status);

            // 2. The JSON Strategy
            // We map the C# property 'ComputedPriority' to a SQLite Generated Column.
            // 'stored: true' means SQLite calculates it on INSERT/UPDATE and saves the result to disk.
            // This costs disk space but makes SELECT instantaneous.
            entity.Property(e => e.ComputedPriority)
                  .HasComputedColumnSql("json_extract(CustomFieldsJson, '$.priority_score')", stored: true);

            entity.Property(e => e.ComputedCategory)
                  .HasComputedColumnSql("json_extract(CustomFieldsJson, '$.category')", stored: true);

            // 3. The Index
            // NOW we can create a standard SQL index on a JSON field.
            // Query: SELECT * FROM Tickets WHERE ComputedPriority > 10
            // Plan: SEARCH TABLE Tickets USING INDEX IX_Tickets_ComputedPriority
            entity.HasIndex(e => e.ComputedPriority);
            entity.HasIndex(e => e.ComputedCategory);
        });
    }

    // Optional: Safety check to ensure we are using the correct provider
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
             // Fallback for dev time only
             optionsBuilder.UseSqlite("Data Source=masala.db;Cache=Shared");
        }
    }
}

3. Visualizing the Physical Layout

Here is what is happening on the disk. This is why we beat the "Microservice" teams. They are serializing/deserializing across the network. We are reading a B-Tree from a local file.
Code snippet

classDiagram
    class TicketTable {
        +PK Id (String)
        +DomainId (String)
        +CustomFieldsJson (TEXT)
        +ComputedPriority (REAL) [GENERATED]
    }

    class DiskStorage {
        Main B-Tree (Table Data)
        Index B-Tree (ComputedPriority)
    }

    TicketTable --> DiskStorage : Stores Data
    
    note for TicketTable "The ComputedPriority column\nis automatically updated by SQLite\nwhenever CustomFieldsJson changes."
    note for DiskStorage "The Index B-Tree allows\nO(log n) lookups on JSON data\nwithout scanning the JSON text."

4. Implementation Directives

    Strict JSON Paths: The paths $.priority_score and $.category are hardcoded in the EF Core mapping above. This means your masala_domains.yaml MUST output these specific JSON keys if you want them indexed.

    Migration Required: Run dotnet ef migrations add InitialSchema. Verify the generated SQL contains GENERATED ALWAYS AS (...) STORED.

    WAL Mode: When you register this in Program.cs, the connection string must be: "Data Source=masala.db;Mode=ReadWriteCreate;Cache=Shared;Foreign Keys=True;Pooling=True;" Note: You must also execute PRAGMA journal_mode = WAL; explicitly on startup.

Decision Point: The current ComputedColumnSql hardcodes $.priority_score.

    Option A (Static): We keep it hardcoded. Simpler, but limits us to indexing only what we decided at compile time.

    Option B (Dynamic): We don't use EF Core for the generated columns. We use raw SQL migrations to add generated columns based on the config.

As the Architect, I dictate Option A for Version 1. "Premature optimization is the root of all evil." Let's index the 2-3 most critical metrics (Priority, Category, DueDate) and let the rest live in the JSON blob.

Do you accept this schema, or do you want to contest the hardcoded JSON paths?
