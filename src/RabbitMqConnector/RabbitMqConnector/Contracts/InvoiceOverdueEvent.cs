using System.Text.Json.Serialization;

namespace RabbitMqConnector.Contracts;

public record InvoiceOverdueEvent : IEvent
{
    [JsonPropertyName("event_type")]
    public string EventType { get; init; } = "invoice.overdue";

    [JsonPropertyName("invoice_id")]
    public string InvoiceId { get; init; } = string.Empty;

    [JsonPropertyName("odoo_invoice_id")]
    public string OdooInvoiceId { get; init; } = string.Empty;

    [JsonPropertyName("customer_email")]
    public string CustomerEmail { get; init; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("days_overdue")]
    public int DaysOverdue { get; init; }

    [JsonPropertyName("event_id")]
    public string EventId { get; init; } = Guid.NewGuid().ToString();
}
