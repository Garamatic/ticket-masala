using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Domain.Entities;
using TicketMasala.Web.Domain.Entities.Configuration; // For DomainConfigVersion
using TicketMasala.Web.Models;
using System.Data.Common;

namespace TicketMasala.Web.Data;

public class MasalaDbContext : DbContext
{
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<DomainConfigVersion> DomainConfigVersions { get; set; }

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

            // 2. The JSON Strategy (Generated Columns)
            entity.Property(e => e.ComputedPriority)
                  .HasComputedColumnSql("json_extract(CustomFieldsJson, '$.priority_score')", stored: true);

            entity.Property(e => e.ComputedCategory)
                  .HasComputedColumnSql("json_extract(CustomFieldsJson, '$.category')", stored: true);

            // Index the generated columns
            entity.HasIndex(e => e.ComputedPriority);
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