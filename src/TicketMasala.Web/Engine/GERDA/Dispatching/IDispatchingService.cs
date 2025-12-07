namespace TicketMasala.Web.Engine.GERDA.Dispatching;

/// <summary>
/// D - Dispatching: Agent-ticket matching using ML.NET recommendation
/// Recommends the best agent for a ticket based on historical affinity and workload.
/// </summary>
public interface IDispatchingService
{
    /// <summary>
    /// Get recommended agent for a ticket
    /// </summary>
    /// <param name="ticketGuid">The ticket Guid to assign</param>
    /// <returns>Recommended employee user ID, or null if no recommendation</returns>
    Task<string?> GetRecommendedAgentAsync(Guid ticketGuid);
    
    /// <summary>
    /// Get top N recommended agents for a ticket
    /// </summary>
    Task<List<(string AgentId, double Score)>> GetTopRecommendedAgentsAsync(Guid ticketGuid, int count = 3);
    
    /// <summary>
    /// Auto-dispatch a ticket to the best available agent
    /// </summary>
    Task<bool> AutoDispatchTicketAsync(Guid ticketGuid);
    
    /// <summary>
    /// Retrain the recommendation model
    /// </summary>
    Task RetrainModelAsync();
    
    /// <summary>
    /// Get recommended project manager for a ticket/project
    /// Uses same affinity model but filters to employees with PM capabilities
    /// </summary>
    /// <param name="ticketGuid">The ticket to find a PM for</param>
    /// <returns>Recommended PM user ID, or null if no recommendation</returns>
    Task<string?> GetRecommendedProjectManagerAsync(Guid ticketGuid);
    
    /// <summary>
    /// Check if dispatching is enabled
    /// </summary>
    bool IsEnabled { get; }

}
