using System.Text.Json.Serialization;

namespace TicketMasala.Web.ViewModels.Tickets;

public class ExternalTicketRequest
{
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? SourceSite { get; set; }
}

public class ExternalTicketResponse
{
    public bool Success { get; set; }

    [JsonPropertyName("ticket_id")]
    public string? TicketId { get; set; }

    public string? Message { get; set; }

    [JsonPropertyName("reference_number")]
    public string? ReferenceNumber { get; set; }
}

