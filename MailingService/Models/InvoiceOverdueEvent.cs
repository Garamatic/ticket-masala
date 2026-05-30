using System.Text.Json.Serialization;

namespace MailingService.Models;

public class InvoiceOverdueEvent : IEvent
{
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("invoice_id")]
    public Guid InvoiceId { get; set; }

    [JsonPropertyName("odoo_invoice_id")]
    public string OdooInvoiceId { get; set; } = string.Empty;

    [JsonPropertyName("customer_email")]
    public string CustomerEmail { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("days_overdue")]
    public int DaysOverdue { get; set; }
}