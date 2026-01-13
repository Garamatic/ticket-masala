using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Data.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Data;

/// <summary>
/// Database seeder orchestrator using Strategy Pattern.
/// Delegates seeding logic to focused ISeedStrategy implementations.
/// Reduced from 620 lines to ~100 lines using decomposition.
/// </summary>
public class DbSeeder
{
    private readonly IEnumerable<ISeedStrategy> _seedStrategies;
    private readonly MasalaDbContext _context;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(
        IEnumerable<ISeedStrategy> seedStrategies,
        MasalaDbContext context,
        ILogger<DbSeeder> logger)
    {
        _seedStrategies = seedStrategies;
        _context = context;
        _logger = logger;
    }

    public async Task<bool> CheckTablesExistAsync()
    {
        try
        {
            await _context.Database.CanConnectAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task SeedAsync()
    {
        Console.WriteLine("========== DATABASE SEEDING STARTED ==========");
        _logger.LogInformation("========== DATABASE SEEDING STARTED ==========");

        // Apply pending migrations (EF Core only)
        try
        {
            Console.WriteLine("Applying pending database migrations...");
            _logger.LogInformation("Applying pending database migrations...");
            if (_context.Database.IsRelational())
            {
                await _context.Database.MigrateAsync();
                Console.WriteLine("Migrations applied successfully.");
            }
            else
            {
                _logger.LogInformation("Non-relational provider detected; skipping migrations");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error applying migrations: {ex}");
            _logger.LogError(ex, "Error applying migrations");
        }

        // Early exit optimization: Check if database is fully seeded
        var userCount = await _context.Users.CountAsync();
        var kbCount = await _context.KnowledgeBaseArticles.CountAsync();

        Console.WriteLine($"Current Database Status - Users: {userCount}, KB Articles: {kbCount}");
        _logger.LogInformation("Current Database Status - Users: {UserCount}, KB Articles: {KbCount}", userCount, kbCount);

        // We removed the global early exit here to allow individual strategies (like UserSeedStrategy)
        // to determine if they need to run updates (e.g., password resets, new roles).
        // Each strategy is now responsible for its own idempotency checks.

        // Execute all seed strategies in order
        foreach (var strategy in _seedStrategies)
        {
            var strategyName = strategy.GetType().Name;

            try
            {
                if (await strategy.ShouldSeedAsync())
                {
                    Console.WriteLine($"Executing seed strategy: {strategyName}");
                    _logger.LogInformation("Executing seed strategy: {Strategy}", strategyName);
                    await strategy.SeedAsync();
                }
                else
                {
                    Console.WriteLine($"Skipping seed strategy (already seeded): {strategyName}");
                    _logger.LogDebug("Skipping seed strategy (already seeded): {Strategy}", strategyName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing seed strategy {strategyName}: {ex}");
                _logger.LogError(ex, "Error executing seed strategy: {Strategy}", strategyName);
                // Continue with other strategies even if one fails
            }
        }

        Console.WriteLine("========== DATABASE SEEDING COMPLETED ==========");
        _logger.LogInformation("========== DATABASE SEEDING COMPLETED ==========");
    }
}
