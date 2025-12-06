using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Models;
using System.Data.Common;

namespace TicketMasala.Web.Data;

public class MasalaDbContext : DbContext
{
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<WorkItem> WorkItems { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<ApplicationUser> Users { get; set; }
    // Backwards-compatible DbSets: older code expects `Customers` and `Employees`.
    // Keep these mapped to the same entity types to minimize refactor churn.
    public DbSet<ApplicationUser> Customers { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<KnowledgeBaseArticle> KnowledgeBaseArticles { get; set; }
    public DbSet<ProjectTemplate> ProjectTemplates { get; set; }
    public DbSet<TemplateTicket> TemplateTickets { get; set; }
    public DbSet<SavedFilter> SavedFilters { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<TicketComment> TicketComments { get; set; }
    public DbSet<QualityReview> QualityReviews { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<AuditLogEntry> AuditLogs { get; set; }

    public MasalaDbContext(DbContextOptions<MasalaDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Ticket Configuration
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Tickets");

            // Indexes for core lookups
            entity.HasIndex(e => e.DomainId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ContentHash); // Fast duplicate check


        });

        // 2. Config Versioning
        modelBuilder.Entity<DomainConfigVersion>(entity =>
        {
            entity.HasIndex(e => e.Hash).IsUnique();
        });

        // 3. WorkItem Configuration
        modelBuilder.Entity<WorkItem>(entity =>
        {
            // The JSON Blob
            entity.Property(e => e.Payload).HasColumnType("TEXT");

            // VITAL: The Virtual Column for Indexing
            entity.Property(e => e.Status)
                  .HasComputedColumnSql("json_extract(Payload, '$.Status')", stored: false);

            // The Index that saves us from Table Scans
            entity.HasIndex(e => e.Status);
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder.AddInterceptors(new SQLitePragmaInterceptor());
    }

    private class SQLitePragmaInterceptor : DbCommandInterceptor
    {
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            if (command.Connection is SqliteConnection sqliteConnection)
            {
                sqliteConnection.Open();
                using var pragmaCommand = sqliteConnection.CreateCommand();
                pragmaCommand.CommandText = "PRAGMA journal_mode=WAL;";
                pragmaCommand.ExecuteNonQuery();
            }

            return base.ReaderExecuting(command, eventData, result);
        }
    }
}