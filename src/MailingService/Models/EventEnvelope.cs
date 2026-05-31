using System.Text.Json.Serialization;

namespace MailingService.Models;

public class EventEnvelope
{
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;
}