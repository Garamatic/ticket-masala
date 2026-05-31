using System.Text.Json.Serialization;

namespace MailingService.Models;

public class TicketAssignedEvent : IEvent
{
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("ticket_id")]
    public Guid TicketId { get; set; }

    [JsonPropertyName("customer_email")]
    public string CustomerEmail { get; set; } = string.Empty;

    [JsonPropertyName("assigned_to")]
    public string AssignedTo { get; set; } = string.Empty;

    [JsonPropertyName("assigned_by")]
    public string AssignedBy { get; set; } = string.Empty;

    [JsonPropertyName("assigned_at")]
    public DateTime AssignedAt { get; set; }
}