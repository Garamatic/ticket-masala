using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Models;
using System.Data.Common;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace TicketMasala.Web.Data;

public class MasalaDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public DbSet<Ticket> Tickets { get; set; }

    public DbSet<Project> Projects { get; set; }
    // Users DbSet is provided by IdentityDbContext
    // public DbSet<ApplicationUser> Users { get; set; }
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
    public DbSet<TimeLog> TimeLogs { get; set; }

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


            // 3. The JSON Strategy
            // We map the C# property 'ComputedPriority' to a SQLite Generated Column.
            // 'stored: true' means SQLite calculates it on INSERT/UPDATE and saves the result to disk.
            // This costs disk space but makes SELECT instantaneous.
            entity.Property(e => e.ComputedPriority)
                  .HasComputedColumnSql("json_extract(CustomFieldsJson, '$.priority_score')", stored: true);

            entity.Property(e => e.ComputedCategory)
                  .HasComputedColumnSql("json_extract(CustomFieldsJson, '$.category')", stored: true);

            // 4. The Index
            // NOW we can create a standard SQL index on a JSON field.
            // Query: SELECT * FROM Tickets WHERE ComputedPriority > 10
            // Plan: SEARCH TABLE Tickets USING INDEX IX_Tickets_ComputedPriority
            entity.HasIndex(e => e.ComputedPriority);
            entity.HasIndex(e => e.ComputedCategory);
        });

        // 2. Config Versioning
        modelBuilder.Entity<DomainConfigVersion>(entity =>
        {
            entity.HasIndex(e => e.Hash).IsUnique();
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