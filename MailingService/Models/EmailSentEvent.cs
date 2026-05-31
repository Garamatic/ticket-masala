using System.Text.Json.Serialization;

namespace MailingService.Models;

public class EmailSendEvent
{
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("to_email")]
    public string ToEmail { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("body_html")]
    public string BodyHtml { get; set; } = string.Empty;

    [JsonPropertyName("from_email")]
    public string FromEmail { get; set; } = string.Empty;

    [JsonPropertyName("from_name")]
    public string FromName { get; set; } = string.Empty;

    [JsonPropertyName("reply_to")]
    public string? ReplyTo { get; set; }
}