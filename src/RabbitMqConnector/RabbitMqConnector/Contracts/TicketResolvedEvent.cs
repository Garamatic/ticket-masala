using System.Text.Json.Serialization;

namespace RabbitMqConnector.Contracts;

public record TicketResolvedEvent : IEvent
{
    [JsonPropertyName("event_type")]
    public string EventType { get; init; } = "ticket.resolved";

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = DateTime.UtcNow.ToString("o");

    [JsonPropertyName("source")]
    public string Source { get; init; } = "ticket-masala";

    [JsonPropertyName("ticket_id")]
    public string TicketId { get; init; } = string.Empty;

    [JsonPropertyName("customer_email")]
    public string CustomerEmail { get; init; } = string.Empty;

    [JsonPropertyName("customer_name")]
    public string CustomerName { get; init; } = string.Empty;

    [JsonPropertyName("service_description")]
    public string ServiceDescription { get; init; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("tenant_id")]
    public string TenantId { get; init; } = string.Empty;

    [JsonPropertyName("resolved_at")]
    public string ResolvedAt { get; init; } = DateTime.UtcNow.ToString("o");

    [JsonPropertyName("resolution_notes")]
    public string? ResolutionNotes { get; init; }

    [JsonPropertyName("event_id")]
    public string EventId { get; init; } = Guid.NewGuid().ToString();
}
