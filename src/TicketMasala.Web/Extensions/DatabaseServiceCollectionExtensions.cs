using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TicketMasala.Web.Data;
using TicketMasala.Web.Tenancy;

namespace TicketMasala.Web.Extensions;

/// <summary>
/// Extension methods for configuring database services.
/// </summary>
public static class DatabaseServiceCollectionExtensions
{
    /// <summary>
    /// Adds database context with SQLite or SQL Server support based on configuration.
    /// </summary>
    public static IServiceCollection AddMasalaDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        if (environment.IsEnvironment("Testing"))
        {
            return services; // Skip DB registration in test environment
        }

        var dbProvider = configuration["DatabaseProvider"];
        var tenantConnectionResolver = new TenantConnectionResolver(configuration);
        var connectionString = tenantConnectionResolver.GetCurrentConnectionString();

        services.AddDbContext<MasalaDbContext>(options =>
        {
            if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                ConfigureSqlite(options, connectionString);
            }
            else
            {
                ConfigureSqlServer(options, connectionString);
            }
        });

        // Register TenantConnectionResolver for DI
        services.AddSingleton<TenantConnectionResolver>();

        return services;
    }

    private static void ConfigureSqlite(DbContextOptionsBuilder options, string? connectionString)
    {
        if (connectionString != null)
        {
            // Ensure data directory exists for SQLite in containerized environments
            var match = System.Text.RegularExpressions.Regex.Match(
                connectionString, @"Data Source=([^;]+)");
            if (match.Success)
            {
                var dbPath = match.Groups[1].Value;
                var dataDir = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(dataDir) && !Directory.Exists(dataDir))
                {
                    Console.WriteLine($"Creating database directory: {dataDir}");
                    Directory.CreateDirectory(dataDir);
                }
            }
        }

        Console.WriteLine($"Using SQLite Provider with connection: {connectionString}");
        options.UseSqlite(connectionString);
        options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    private static void ConfigureSqlServer(DbContextOptionsBuilder options, string? connectionString)
    {
        Console.WriteLine($"Using SQL Server Provider");
        options.UseSqlServer(connectionString, sqlServerOptions =>
        {
            sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });
    }
}
