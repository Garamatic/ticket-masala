namespace TicketMasala.Web.Engine.GERDA.Grouping;

/// <summary>
/// G - Grouping: Spam detection and ticket clustering
/// Detects when a client submits multiple similar tickets in a short time window
/// and groups them into a single parent ticket.
/// </summary>
public interface IGroupingService
{
    /// <summary>
    /// Check if a ticket should be grouped with existing tickets from the same requester
    /// </summary>
    /// <param name="ticketGuid">The ticket Guid to check</param>
    /// <returns>Parent ticket Guid if grouped, null if not grouped</returns>
    Task<Guid?> CheckAndGroupTicketAsync(Guid ticketGuid);
    
    /// <summary>
    /// Get all tickets that could be grouped for a specific customer
    /// </summary>
    Task<List<Guid>> GetGroupableTicketsAsync(string customerId, int timeWindowMinutes);
    
    /// <summary>
    /// Check if grouping/spam detection is enabled
    /// </summary>
    bool IsEnabled { get; }

}
