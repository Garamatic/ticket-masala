using System.Text.Json.Serialization;

namespace RabbitMqConnector.Contracts;

public record PaymentReceivedEvent : IEvent
{
    [JsonPropertyName("event_type")]
    public string EventType { get; init; } = "payment.received";

    [JsonPropertyName("invoice_id")]
    public string InvoiceId { get; init; } = string.Empty;

    [JsonPropertyName("odoo_invoice_id")]
    public string OdooInvoiceId { get; init; } = string.Empty;

    [JsonPropertyName("customer_email")]
    public string CustomerEmail { get; init; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("payment_method")]
    public string PaymentMethod { get; init; } = string.Empty;

    [JsonPropertyName("paid_at")]
    public string PaidAt { get; init; } = DateTime.UtcNow.ToString("o");

    [JsonPropertyName("event_id")]
    public string EventId { get; init; } = Guid.NewGuid().ToString();
}
