using System.Text.Json.Serialization;

namespace RabbitMqConnector.Contracts;

public record UserCreatedEvent : IEvent
{
    [JsonPropertyName("event_type")]
    public string EventType { get; init; } = "user.created";

    [JsonPropertyName("user_id")]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("tenant_id")]
    public string TenantId { get; init; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; init; } = DateTime.UtcNow.ToString("o");

    [JsonPropertyName("event_id")]
    public string EventId { get; init; } = Guid.NewGuid().ToString();
}
