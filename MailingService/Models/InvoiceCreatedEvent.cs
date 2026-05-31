using System.Text.Json.Serialization;

namespace MailingService.Models;

//TODO: Update to real model
public class InvoiceCreatedEvent : IEvent
{
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("invoice_id")]
    public Guid? InvoiceId { get; set; }

    [JsonPropertyName("odoo_invoice_id")]
    public string OdooInvoiceId { get; set; } = string.Empty;

    [JsonPropertyName("ticket_id")]
    public Guid TicketId { get; set; }

    [JsonPropertyName("customer_email")]
    public string CustomerEmail { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}