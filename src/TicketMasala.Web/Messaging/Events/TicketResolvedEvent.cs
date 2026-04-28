namespace TicketMasala.Web.Messaging.Events;

/// <summary>
/// Flat snake_case event matching odoo-integration consumer and integration-contracts schema.
/// Serialized via System.Text.Json with SnakeCaseLower naming policy.
/// </summary>
public record TicketResolvedEvent
{
    public string EventType { get; init; } = "ticket.resolved";
    public string TicketId { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string ServiceDescription { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public DateTime ResolvedAt { get; init; }
    public string? ResolutionNotes { get; init; }
    public string EventId { get; init; } = Guid.NewGuid().ToString();
}
