using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RabbitMqConnector.Interfaces;

namespace IT_Project2526.RabbitMQ.RabbitMQDTOs
{

    public abstract class BaseEventDTO
    {
        [JsonPropertyName("event_type")]
        [Required]
        public abstract string EventType { get; }
    }
    public abstract class BaseTicketEventDTO : BaseEventDTO
    {
        [JsonPropertyName("ticket_id")]
        [Required]
        public Guid TicketId { get; set; }
    }

    public abstract class BaseInvoiceEventDTO : BaseEventDTO
    {
        [JsonPropertyName("invoice_id")]
        public Guid? InvoiceId { get; set; }

        [JsonPropertyName("odoo_invoice_id")]
        public string OdooInvoiceId { get; set; } = string.Empty;

        [JsonPropertyName("customer_email")]
        [EmailAddress]
        public string CustomerEmail { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }
    }
    [RabbitExchange("ticket-created", "topic")]
    public class TicketCreatedDTO : BaseTicketEventDTO, IProducer
    {
        public override string EventType => "ticket.created";

        [JsonPropertyName("customer_email")]
        [EmailAddress]
        public string CustomerEmail { get; set; } = string.Empty;

        [JsonPropertyName("customer_name")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("tenant_id")]
        public string TenantId { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "medium"; // low, medium, high, urgent

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; }
    }
    [RabbitExchange("ticket-assigned", "topic")]
    public class TicketAssignedDTO : BaseTicketEventDTO, IProducer
    {
        public override string EventType => "ticket.assigned";

        [JsonPropertyName("assigned_to")]
        public string AssignedTo { get; set; } = string.Empty;

        [JsonPropertyName("assigned_by")]
        public string AssignedBy { get; set; } = string.Empty;

        [JsonPropertyName("assigned_at")]
        public DateTimeOffset AssignedAt { get; set; }
    }
    [RabbitExchange("ticket-resolved", "topic")]
    public class TicketResolvedDTO : BaseTicketEventDTO, IProducer
    {
        public override string EventType => "ticket.resolved";

        [JsonPropertyName("customer_email")]
        [EmailAddress]
        public string CustomerEmail { get; set; } = string.Empty;

        [JsonPropertyName("customer_name")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("service_description")]
        public string ServiceDescription { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("tenant_id")]
        public string TenantId { get; set; } = string.Empty;

        [JsonPropertyName("resolved_at")]
        public DateTimeOffset ResolvedAt { get; set; }

        [JsonPropertyName("resolution_notes")]
        public string? ResolutionNotes { get; set; }
    }
    [RabbitExchange("invoice-requested", "topic")]
    public class InvoiceCreateRequestedDTO : BaseTicketEventDTO, IProducer
    {
        public override string EventType => "invoice.create_requested";

        [JsonPropertyName("customer_email")]
        [EmailAddress]
        public string CustomerEmail { get; set; } = string.Empty;

        [JsonPropertyName("customer_name")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("requested_at")]
        public DateTimeOffset RequestedAt { get; set; }
    }

}
