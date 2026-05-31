using System.Text.Json.Serialization;

namespace RabbitMqConnector.Contracts;

public record TicketAssignedEvent : IEvent
{
    [JsonPropertyName("event_type")]
    public string EventType { get; init; } = "ticket.assigned";

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

    [JsonPropertyName("assigned_to")]
    public string AssignedTo { get; init; } = string.Empty;

    [JsonPropertyName("assigned_by")]
    public string AssignedBy { get; init; } = string.Empty;

    [JsonPropertyName("assigned_at")]
    public string AssignedAt { get; init; } = DateTime.UtcNow.ToString("o");

    [JsonPropertyName("event_id")]
    public string EventId { get; init; } = Guid.NewGuid().ToString();
}
