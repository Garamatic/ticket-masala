namespace TicketMasala.Web.Engine.GERDA.Estimating;

/// <summary>
/// E - Estimating: Complexity estimation using Fibonacci points
/// Predicts effort/complexity based on ticket category and historical data.
/// </summary>
public interface IEstimatingService
{
    /// <summary>
    /// Estimate the complexity of a ticket in Fibonacci points (1, 2, 3, 5, 8, 13, 21)
    /// </summary>
    /// <param name="ticketGuid">The ticket Guid to estimate</param>
    /// <returns>Fibonacci point value</returns>
    Task<int> EstimateComplexityAsync(Guid ticketGuid);

    /// <summary>
    /// Get complexity estimate based on category name
    /// </summary>
    int GetComplexityByCategory(string category);

    /// <summary>
    /// Check if estimation is enabled
    /// </summary>
    bool IsEnabled { get; }

}
