using System.Text.Json.Serialization;

namespace RabbitMqConnector.Contracts;

/// <summary>
/// Shared integration contract for the ticket.created event.
/// Serialized as snake_case JSON to match integration-contracts schema.
/// </summary>
public record TicketCreatedEvent : IEvent
{
    [JsonPropertyName("event_type")]
    public string EventType { get; init; } = "ticket.created";

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

    [JsonPropertyName("tenant_id")]
    public string TenantId { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; init; } = "medium";

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; init; } = DateTime.UtcNow.ToString("o");

    [JsonPropertyName("event_id")]
    public string EventId { get; init; } = Guid.NewGuid().ToString();
}
