# Quick Fixes: Database Provider Abstraction

**Priority:** 🔴 Critical  
**Estimated Time:** 2-3 hours

---

## Problem

The `MasalaDbContext` uses SQLite-specific SQL syntax for computed columns, preventing migration to SQL Server or PostgreSQL:

```csharp
// Current code (SQLite-only)
entity.Property(e => e.ComputedPriority)
      .HasComputedColumnSql("json_extract(CustomFieldsJson, '$.priority_score')", stored: true);
```

---

## Solution: Database Provider Factory

Create a provider-agnostic abstraction that detects the database provider and uses appropriate SQL syntax.

### Step 1: Create Database Provider Helper

**File:** `src/TicketMasala.Web/Data/DatabaseProviderHelper.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace TicketMasala.Web.Data;

/// <summary>
/// Helper class for database provider-specific SQL generation.
/// </summary>
public static class DatabaseProviderHelper
{
    /// <summary>
    /// Gets the database provider name from the DbContext options.
    /// </summary>
    public static string GetProviderName(DbContextOptions options)
    {
        var extension = options.Extensions.OfType<RelationalOptionsExtension>().FirstOrDefault();
        return extension?.ProviderName ?? "Unknown";
    }

    /// <summary>
    /// Gets the JSON extraction SQL for the current database provider.
    /// </summary>
    public static string GetJsonExtractSql(string jsonPath, string columnName)
    {
        // This will be called during model building, so we need to detect provider differently
        // For now, use environment variable or configuration
        var provider = Environment.GetEnvironmentVariable("DatabaseProvider") 
                    ?? "SQLite"; // Default to SQLite for backward compatibility

        return provider.ToUpperInvariant() switch
        {
            "SQLITE" => $"json_extract({columnName}, '{jsonPath}')",
            "SQLSERVER" or "MSSQL" => $"JSON_VALUE({columnName}, '{jsonPath}')",
            "POSTGRESQL" or "NPGSQL" => $"({columnName}->>'{jsonPath.Replace("$.", "")}')::float",
            _ => $"json_extract({columnName}, '{jsonPath}')" // Default to SQLite
        };
    }

    /// <summary>
    /// Configures computed columns for Ticket entity based on database provider.
    /// </summary>
    public static void ConfigureTicketComputedColumns(
        EntityTypeBuilder<Ticket> entity, 
        string providerName)
    {
        var provider = providerName.ToUpperInvariant();

        // ComputedPriority
        var prioritySql = provider switch
        {
            "SQLITE" => "json_extract(CustomFieldsJson, '$.priority_score')",
            "SQLSERVER" or "MSSQL" => "JSON_VALUE(CustomFieldsJson, '$.priority_score')",
            "POSTGRESQL" or "NPGSQL" => "(CustomFieldsJson->>'priority_score')::float",
            _ => "json_extract(CustomFieldsJson, '$.priority_score')"
        };

        entity.Property(e => e.ComputedPriority)
              .HasComputedColumnSql(prioritySql, stored: true);

        // ComputedCategory
        var categorySql = provider switch
        {
            "SQLITE" => "json_extract(CustomFieldsJson, '$.category')",
            "SQLSERVER" or "MSSQL" => "JSON_VALUE(CustomFieldsJson, '$.category')",
            "POSTGRESQL" or "NPGSQL" => "CustomFieldsJson->>'category'",
            _ => "json_extract(CustomFieldsJson, '$.category')"
        };

        entity.Property(e => e.ComputedCategory)
              .HasComputedColumnSql(categorySql, stored: true);
    }
}
```

### Step 2: Update MasalaDbContext

**File:** `src/TicketMasala.Web/Data/MasalaDbContext.cs`

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Detect database provider
    var providerName = Database.ProviderName; // EF Core provides this
    
    // 1. Ticket Configuration
    modelBuilder.Entity<Ticket>(entity =>
    {
        entity.ToTable("Tickets");

        // Indexes for core lookups
        entity.HasIndex(e => e.DomainId);
        entity.HasIndex(e => e.Status);
        entity.HasIndex(e => e.ContentHash);

        // Use provider-agnostic computed columns
        DatabaseProviderHelper.ConfigureTicketComputedColumns(entity, providerName);

        // Indexes on computed columns
        entity.HasIndex(e => e.ComputedPriority);
        entity.HasIndex(e => e.ComputedCategory);
    });

    // 2. Config Versioning
    modelBuilder.Entity<DomainConfigVersion>(entity =>
    {
        entity.HasIndex(e => e.Hash).IsUnique();
    });
}
```

### Step 3: Update Database Configuration

**File:** `src/TicketMasala.Web/Extensions/DatabaseServiceCollectionExtensions.cs`

```csharp
private static void ConfigureSqlite(DbContextOptionsBuilder options, string? connectionString)
{
    // ... existing code ...
    
    // Set environment variable for provider detection
    Environment.SetEnvironmentVariable("DatabaseProvider", "SQLite");
    
    options.UseSqlite(connectionString);
    // ... rest of code ...
}

private static void ConfigureSqlServer(DbContextOptionsBuilder options, string? connectionString)
{
    // Set environment variable for provider detection
    Environment.SetEnvironmentVariable("DatabaseProvider", "SqlServer");
    
    options.UseSqlServer(connectionString, sqlServerOptions =>
    {
        // ... existing code ...
    });
}
```

### Step 4: Update SQLite Interceptor (Make Provider-Aware)

**File:** `src/TicketMasala.Web/Data/MasalaDbContext.cs`

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    base.OnConfiguring(optionsBuilder);

    // Only add SQLite interceptor if using SQLite
    if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
    {
        optionsBuilder.AddInterceptors(new SQLitePragmaInterceptor());
    }
}
```

---

## Testing

### Test with SQLite (Default)
```bash
export DatabaseProvider=SQLite
dotnet run --project src/TicketMasala.Web
```

### Test with SQL Server
```bash
export DatabaseProvider=SqlServer
export ConnectionStrings__DefaultConnection="Server=localhost;Database=TicketMasala;..."
dotnet run --project src/TicketMasala.Web
```

---

## Alternative: Use EF Core's Database Provider Detection

For a more robust solution, use EF Core's built-in provider detection:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<Ticket>(entity =>
    {
        // ... other configuration ...

        // Detect provider at model building time
        var provider = this.Database.ProviderName;
        
        if (provider.Contains("Sqlite"))
        {
            entity.Property(e => e.ComputedPriority)
                  .HasComputedColumnSql("json_extract(CustomFieldsJson, '$.priority_score')", stored: true);
        }
        else if (provider.Contains("SqlServer"))
        {
            entity.Property(e => e.ComputedPriority)
                  .HasComputedColumnSql("JSON_VALUE(CustomFieldsJson, '$.priority_score')", stored: true);
        }
        // Add PostgreSQL support as needed
    });
}
```

**Note:** The challenge is that `OnModelCreating` is called during model building, before the database connection is established. The `Database.ProviderName` property may not be available at that time.

**Better Approach:** Use a factory pattern with lazy initialization:

```csharp
public class MasalaDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    private string? _providerName;

    private string ProviderName => _providerName ??= Database.ProviderName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Use ProviderName here - it will be evaluated lazily
        // But note: this still may not work if called before connection
        
        // Best solution: Pass provider name via constructor or options
    }
}
```

---

## Recommended Final Solution

**Pass provider name via DbContextOptions:**

1. Create custom extension method:
```csharp
public static class MasalaDbContextOptionsExtensions
{
    public static DbContextOptionsBuilder<TContext> WithProviderName<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        string providerName)
        where TContext : DbContext
    {
        optionsBuilder.AddExtension(new ProviderNameExtension(providerName));
        return optionsBuilder;
    }
}

public class ProviderNameExtension : IDbContextOptionsExtension
{
    public string ProviderName { get; }

    public ProviderNameExtension(string providerName)
    {
        ProviderName = providerName;
    }

    public void ApplyServices(IServiceCollection services) { }
    public void Validate(IDbContextOptions options) { }
}
```

2. Use in configuration:
```csharp
services.AddDbContext<MasalaDbContext>(options =>
{
    if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString)
               .WithProviderName("SQLite");
    }
    else
    {
        options.UseSqlServer(connectionString)
               .WithProviderName("SqlServer");
    }
});
```

3. Access in DbContext:
```csharp
private string GetProviderName()
{
    return this.GetService<ProviderNameExtension>()?.ProviderName 
           ?? Database.ProviderName;
}
```

---

## Summary

**Quick Fix (Recommended for immediate use):**
- Use environment variable `DatabaseProvider`
- Create `DatabaseProviderHelper` utility class
- Update `OnModelCreating` to use helper

**Long-term Fix:**
- Implement provider name extension pattern
- Or use EF Core's `Database.ProviderName` with proper initialization
- Consider using a database abstraction library

---

**Last Updated:** January 2025

