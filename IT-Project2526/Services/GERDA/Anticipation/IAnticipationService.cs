namespace IT_Project2526.Services.GERDA.Anticipation;

/// <summary>
/// A - Anticipation: Capacity forecasting using ML.NET Time Series
/// Predicts future ticket inflow and compares against team capacity.
/// </summary>
public interface IAnticipationService
{
    /// <summary>
    /// Forecast ticket inflow for the next N days
    /// </summary>
    /// <param name="horizonDays">Number of days to forecast</param>
    /// <returns>List of (Date, PredictedCount) tuples</returns>
    Task<List<(DateTime Date, int PredictedCount)>> ForecastInflowAsync(int horizonDays = 30);
    
    /// <summary>
    /// Get current team capacity (tickets per day)
    /// </summary>
    Task<double> GetTeamCapacityAsync(int? projectId = null);
    
    /// <summary>
    /// Check if there's a capacity risk in the forecast horizon
    /// </summary>
    /// <returns>Risk details if risk detected, null if no risk</returns>
    Task<CapacityRiskResult?> CheckCapacityRiskAsync();
    
    /// <summary>
    /// Check if anticipation is enabled
    /// </summary>
    bool IsEnabled { get; }
}

/// <summary>
/// Result of a capacity risk check
/// </summary>
public class CapacityRiskResult
{
    public DateTime RiskStartDate { get; set; }
    public int ForecastedInflow { get; set; }
    public double AvailableCapacity { get; set; }
    public double RiskPercentage { get; set; }
    public string AlertMessage { get; set; } = string.Empty;
}
