namespace TicketMasala.Web.Data.Seeding;

/// <summary>
/// Strategy pattern interface for database seeding.
/// Allows decomposition of the monolithic DbSeeder into focused, testable strategies.
/// </summary>
public interface ISeedStrategy
{
    /// <summary>
    /// Determines if this strategy should execute seeding.
    /// Enables skip-if-exists logic for each strategy independently.
    /// </summary>
    Task<bool> ShouldSeedAsync();

    /// <summary>
    /// Executes the seeding logic for this strategy.
    /// </summary>
    Task SeedAsync();
}
