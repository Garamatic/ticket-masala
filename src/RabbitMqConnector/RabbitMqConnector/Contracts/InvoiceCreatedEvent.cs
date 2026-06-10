using System.Text.Json.Serialization;

namespace RabbitMqConnector.Contracts;

public record InvoiceCreatedEvent : IEvent
{
    [JsonPropertyName("event_type")]
    public string EventType { get; init; } = "invoice.created";

    [JsonPropertyName("invoice_id")]
    public string InvoiceId { get; init; } = string.Empty;

    [JsonPropertyName("odoo_invoice_id")]
    public int OdooInvoiceId { get; init; }

    [JsonPropertyName("ticket_id")]
    public string TicketId { get; init; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = DateTime.UtcNow.ToString("o");

    [JsonPropertyName("source")]
    public string Source { get; init; } = "odoo-integration";

    [JsonPropertyName("customer_email")]
    public string CustomerEmail { get; init; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; init; } = DateTime.UtcNow.ToString("o");

    [JsonPropertyName("event_id")]
    public string EventId { get; init; } = Guid.NewGuid().ToString();
}
