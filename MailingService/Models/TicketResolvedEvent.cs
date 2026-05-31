using System.Text.Json.Serialization;

namespace MailingService.Models;

//TODO: Update to real model
public class TicketResolvedEvent : IEvent
{
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("ticket_id")]
    public Guid TicketId { get; set; }

    [JsonPropertyName("customer_email")]
    public string CustomerEmail { get; set; } = string.Empty;

    [JsonPropertyName("customer_name")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("service_description")]
    public string ServiceDescription { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("tenant_id")]
    public string TenantId { get; set; } = string.Empty;

    [JsonPropertyName("resolved_at")]
    public DateTime ResolvedAt { get; set; }

    [JsonPropertyName("resolution_notes")]
    public string? ResolutionNotes { get; set; }

}