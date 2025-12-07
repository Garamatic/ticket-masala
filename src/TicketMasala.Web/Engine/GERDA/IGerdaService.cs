namespace TicketMasala.Web.Engine.GERDA;

/// <summary>
/// Main GERDA orchestrator interface.
/// GERDA = GovTech Extended Resource Dispatch & Anticipation
/// </summary>
public interface IGerdaService
{
    /// <summary>
    /// Process all GERDA functions for a ticket
    /// </summary>
    Task ProcessTicketAsync(Guid ticketGuid);
    
    /// <summary>
    /// Run batch processing on all open tickets
    /// </summary>
    Task ProcessAllOpenTicketsAsync();
    
    /// <summary>
    /// Check if GERDA is enabled
    /// </summary>
    bool IsEnabled { get; }

}
