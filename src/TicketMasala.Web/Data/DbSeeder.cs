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
        _logger.LogInformation("========== DATABASE SEEDING STARTED ==========");

        // Early exit optimization: Check if database is fully seeded
        var userCount = await _context.Users.CountAsync();
        var kbCount = await _context.KnowledgeBaseArticles.CountAsync();

        if (userCount > 0 && kbCount > 0)
        {
            _logger.LogWarning("Database fully seeded (Users: {UserCount}, KB: {KbCount}). Skipping seed.", userCount, kbCount);
            return;
        }

        // Apply pending migrations (EF Core only)
        try
        {
            _logger.LogInformation("Applying pending database migrations...");
            if (_context.Database.IsRelational())
            {
                await _context.Database.MigrateAsync();
            }
            else
            {
                _logger.LogInformation("Non-relational provider detected; skipping migrations");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying migrations");
        }

        // Execute all seed strategies in order
        foreach (var strategy in _seedStrategies)
        {
            var strategyName = strategy.GetType().Name;

            try
            {
                if (await strategy.ShouldSeedAsync())
                {
                    _logger.LogInformation("Executing seed strategy: {Strategy}", strategyName);
                    await strategy.SeedAsync();
                }
                else
                {
                    _logger.LogDebug("Skipping seed strategy (already seeded): {Strategy}", strategyName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing seed strategy: {Strategy}", strategyName);
                // Continue with other strategies even if one fails
            }
        }

        _logger.LogInformation("========== DATABASE SEEDING COMPLETED ==========");
    }
}
