namespace TicketMasala.Web.ViewModels.Tickets;

/// <summary>
/// Request model for external ticket submission from partner websites
/// </summary>
public class ExternalTicketRequest
{
    /// <summary>
    /// Customer's email address (used to find or create customer)
    /// </summary>
    public required string CustomerEmail { get; set; }

    /// <summary>
    /// Customer's full name
    /// </summary>
    public required string CustomerName { get; set; }

    /// <summary>
    /// Short subject/title for the ticket
    /// </summary>
    public required string Subject { get; set; }

    /// <summary>
    /// Detailed description of the project request
    /// </summary>
    public required string Description { get; set; }

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
