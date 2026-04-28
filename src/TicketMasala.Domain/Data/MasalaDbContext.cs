using System.Data.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Domain.Data;

public class MasalaDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public DbSet<Ticket> Tickets { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    // Users DbSet is provided by IdentityDbContext
    // public DbSet<ApplicationUser> Users { get; set; }
    // Backwards-compatible DbSets: older code expects `Customers` and `Employees`.
    // Keep these mapped to the same entity types to minimize refactor churn.
    public DbSet<ApplicationUser> Customers { get; set; } = null!;
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<KnowledgeBaseArticle> KnowledgeBaseArticles { get; set; } = null!;
    public DbSet<KnowledgeBaseSnippet> KnowledgeBaseSnippets { get; set; } = null!;
    public DbSet<ProjectTemplate> ProjectTemplates { get; set; } = null!;
    public DbSet<TemplateTicket> TemplateTickets { get; set; } = null!;
    public DbSet<SavedFilter> SavedFilters { get; set; } = null!;
    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<TicketComment> TicketComments { get; set; } = null!;
    public DbSet<QualityReview> QualityReviews { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<AuditLogEntry> AuditLogs { get; set; } = null!;
    public DbSet<TimeLog> TimeLogs { get; set; } = null!;
    public DbSet<DomainConfigVersion> DomainConfigVersions { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    public MasalaDbContext(DbContextOptions<MasalaDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Exclude DomainEvent from EF Core mapping (it's not a persisted entity)
        modelBuilder.Ignore<DomainEvent>();

        // Detect database provider for provider-specific SQL generation
        var providerName = Database.ProviderName ?? "Microsoft.EntityFrameworkCore.Sqlite";

        // 1. Ticket Configuration
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Tickets");

            // Indexes for core lookups
            entity.HasIndex(e => e.DomainId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ContentHash); // Fast duplicate check

            // 2. The JSON Strategy with Provider Abstraction
            // We map the C# property 'ComputedPriority' to a database-generated computed column.
            // 'stored: true' means the database calculates it on INSERT/UPDATE and saves the result to disk.
            // This costs disk space but makes SELECT instantaneous.
            // The SQL syntax is provider-specific and handled by DatabaseProviderHelper.
            DatabaseProviderHelper.ConfigureTicketComputedColumns(entity, providerName);

            // 3. The Index
            // NOW we can create a standard SQL index on a JSON field.
            // Query: SELECT * FROM Tickets WHERE ComputedPriority > 10
            // Plan: SEARCH TABLE Tickets USING INDEX IX_Tickets_ComputedPriority
            entity.HasIndex(e => e.ComputedPriority);
            entity.HasIndex(e => e.ComputedCategory);
        });

        // 4. Config Versioning
        modelBuilder.Entity<DomainConfigVersion>(entity =>
        {
            entity.HasIndex(e => e.Hash).IsUnique();
        });

        // Outbox Message Configuration
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");

            // Index for efficient polling of unprocessed messages
            entity.HasIndex(e => new { e.ProcessedAt, e.ScheduledRetryAt, e.RetryCount })
                .HasDatabaseName("IX_OutboxMessages_Pending");

            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.EventType);
        });

        // 5. Project Configuration
        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(e => e.CustomerIds)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                    (c1, c2) => (c1 ?? new List<string>()).SequenceEqual(c2 ?? new List<string>()),
                    c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c == null ? new List<string>() : c.ToList()));
        });

        // 5. Configure navigation properties for ApplicationUser relationships
        // These are configured here since ApplicationUser is in Domain project
        ConfigureUserRelationships(modelBuilder);
    }

    private void ConfigureUserRelationships(ModelBuilder modelBuilder)
    {
        // Ticket -> ApplicationUser relationships
        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Customer)
            .WithMany()
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Responsible)
            .WithMany()
            .HasForeignKey(t => t.ResponsibleId)
            .OnDelete(DeleteBehavior.SetNull);

        // Project -> ApplicationUser relationships
        modelBuilder.Entity<Project>()
            .HasOne(p => p.ProjectManager)
            .WithMany()
            .HasForeignKey(p => p.ProjectManagerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.Customer)
            .WithMany()
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Project -> Customers (Stakeholders)
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Customers)
            .WithOne()
            .HasForeignKey("StakeholderProjectId")
            .OnDelete(DeleteBehavior.SetNull);

        // Project -> Resources (Employees)
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Resources)
            .WithOne()
            .HasForeignKey("ResourceProjectId")
            .OnDelete(DeleteBehavior.SetNull);

        // Explicitly configure Ticket -> Project relationship (Tasks)
        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectGuid)
            .OnDelete(DeleteBehavior.SetNull);

        // Other entity -> ApplicationUser relationships
        modelBuilder.Entity<Notification>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Document>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(d => d.UploaderId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<TicketComment>()
            .HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<TimeLog>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AuditLogEntry>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<KnowledgeBaseArticle>()
            .HasOne(k => k.Author)
            .WithMany()
            .HasForeignKey(k => k.AuthorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<QualityReview>()
            .HasOne(q => q.Reviewer)
            .WithMany()
            .HasForeignKey(q => q.ReviewerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SavedFilter>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Add SQLite-specific interceptor for WAL mode
        // The interceptor itself checks if the connection is SQLite at runtime,
        // so it's safe to add even when using other providers
        optionsBuilder.AddInterceptors(new SQLitePragmaInterceptor());
    }

    private class SQLitePragmaInterceptor : DbConnectionInterceptor
    {
        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            // Only apply SQLite pragma if using SQLite connection
            if (connection is SqliteConnection sqliteConnection)
            {
                using var pragmaCommand = sqliteConnection.CreateCommand();
                pragmaCommand.CommandText = "PRAGMA journal_mode=WAL;";
                pragmaCommand.ExecuteNonQuery();
            }

            base.ConnectionOpened(connection, eventData);
        }

        public override async Task ConnectionOpenedAsync(
            DbConnection connection,
            ConnectionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            // Only apply SQLite pragma if using SQLite connection
            if (connection is SqliteConnection sqliteConnection)
            {
                using var pragmaCommand = sqliteConnection.CreateCommand();
                pragmaCommand.CommandText = "PRAGMA journal_mode=WAL;";
                await pragmaCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
        }
    }
}
