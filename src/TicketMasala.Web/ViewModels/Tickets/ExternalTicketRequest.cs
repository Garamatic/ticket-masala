namespace TicketMasala.Web.ViewModels.Tickets;

/// <summary>
/// Request model for external ticket submission from partner websites
/// </summary>
public class ExternalTicketRequest
{
    /// <summary>
    /// Customer's email address (used to find or create customer)
    /// </summary>
    public string CustomerEmail { get; set; } = string.Empty;

    /// <summary>
    /// Customer's full name
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Short subject/title for the ticket
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the project request
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Identifier for the source website (e.g., "greenscape-landscaping")
    /// </summary>
    public string? SourceSite { get; set; }
}

/// <summary>
/// Response model for external ticket creation
/// </summary>
public class ExternalTicketResponse
{
    public bool Success { get; set; }
    public string? TicketId { get; set; }
    public string? Message { get; set; }
    public string? ReferenceNumber { get; set; }

}
