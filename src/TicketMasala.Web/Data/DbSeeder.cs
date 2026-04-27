using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Data.Seeding;

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

        // Early exit optimization: Check if database is fully seeded
        var userCount = await _context.Users.CountAsync();
        var kbCount = await _context.KnowledgeBaseArticles.CountAsync();

        _logger.LogInformation("Current Database Status - Users: {UserCount}, KB Articles: {KbCount}", userCount, kbCount);

        // We removed the global early exit here to allow individual strategies (like UserSeedStrategy)
        // to determine if they need to run updates (e.g., password resets, new roles).
        // Each strategy is now responsible for its own idempotency checks.

        // Execute all seed strategies in order
        _logger.LogDebug("DbSeeder found {Count} strategies registered", _seedStrategies.Count());
        foreach (var s in _seedStrategies)
        {
            _logger.LogDebug("Registered Strategy: {Strategy}", s.GetType().Name);
        }

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
