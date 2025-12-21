using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Domain.Data;

/// <summary>
/// Helper class for database provider-specific SQL generation.
/// Provides abstraction for computed columns across SQLite, SQL Server, and PostgreSQL.
/// </summary>
public static class DatabaseProviderHelper
{
    /// <summary>
    /// Configures computed columns for Ticket entity based on database provider.
    /// </summary>
    /// <param name="entity">The entity type builder for Ticket</param>
    /// <param name="providerName">The database provider name (e.g., "Microsoft.EntityFrameworkCore.Sqlite")</param>
    public static void ConfigureTicketComputedColumns(
        EntityTypeBuilder<Ticket> entity,
        string providerName)
    {
        // Normalize provider name for comparison
        var provider = providerName.ToUpperInvariant();

        // Configure ComputedPriority based on provider
        var prioritySql = provider switch
        {
            var p when p.Contains("SQLITE") => "json_extract(CustomFieldsJson, '$.priority_score')",
            var p when p.Contains("SQLSERVER") || p.Contains("MSSQL") => "CAST(JSON_VALUE(CustomFieldsJson, '$.priority_score') AS FLOAT)",
            var p when p.Contains("POSTGRESQL") || p.Contains("NPGSQL") => "(CustomFieldsJson->>'priority_score')::float",
            _ => "json_extract(CustomFieldsJson, '$.priority_score')" // Default to SQLite syntax
        };

        entity.Property(e => e.ComputedPriority)
              .HasComputedColumnSql(prioritySql, stored: true);

        // Configure ComputedCategory based on provider
        var categorySql = provider switch
        {
            var p when p.Contains("SQLITE") => "json_extract(CustomFieldsJson, '$.category')",
            var p when p.Contains("SQLSERVER") || p.Contains("MSSQL") => "JSON_VALUE(CustomFieldsJson, '$.category')",
            var p when p.Contains("POSTGRESQL") || p.Contains("NPGSQL") => "CustomFieldsJson->>'category'",
            _ => "json_extract(CustomFieldsJson, '$.category')" // Default to SQLite syntax
        };

        entity.Property(e => e.ComputedCategory)
              .HasComputedColumnSql(categorySql, stored: true);
    }

    /// <summary>
    /// Gets a human-readable provider name for logging.
    /// </summary>
    public static string GetProviderDisplayName(string providerName)
    {
        var provider = providerName.ToUpperInvariant();
        return provider switch
        {
            var p when p.Contains("SQLITE") => "SQLite",
            var p when p.Contains("SQLSERVER") || p.Contains("MSSQL") => "SQL Server",
            var p when p.Contains("POSTGRESQL") || p.Contains("NPGSQL") => "PostgreSQL",
            _ => providerName
        };
    }

    /// <summary>
    /// Checks if the provider supports computed columns.
    /// </summary>
    public static bool SupportsComputedColumns(string providerName)
    {
        var provider = providerName.ToUpperInvariant();
        return provider.Contains("SQLITE") 
            || provider.Contains("SQLSERVER") 
            || provider.Contains("MSSQL")
            || provider.Contains("POSTGRESQL")
            || provider.Contains("NPGSQL");
    }
}
