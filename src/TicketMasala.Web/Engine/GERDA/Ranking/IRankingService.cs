namespace TicketMasala.Web.Engine.GERDA.Ranking;

/// <summary>
/// R - Ranking: WSJF (Weighted Shortest Job First) priority calculation
/// Calculates priority score: Cost of Delay / Job Size
/// </summary>
public interface IRankingService
{
    /// <summary>
    /// Calculate the priority score for a ticket using WSJF
    /// </summary>
    /// <param name="ticketGuid">The ticket Guid to rank</param>
    /// <returns>Priority score (higher = more urgent)</returns>
    Task<double> CalculatePriorityScoreAsync(Guid ticketGuid);
    
    /// <summary>
    /// Recalculate priority scores for all open tickets
    /// </summary>
    Task RecalculateAllPrioritiesAsync();
    
    /// <summary>
    /// Get tickets ordered by priority score
    /// </summary>
    Task<List<Guid>> GetPrioritizedTicketGuidsAsync(Guid? projectGuid = null);
    
    /// <summary>
    /// Check if ranking is enabled
    /// </summary>
    bool IsEnabled { get; }

}
