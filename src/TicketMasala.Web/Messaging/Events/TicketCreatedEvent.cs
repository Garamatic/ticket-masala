namespace TicketMasala.Web.Messaging.Events;

/// <summary>
/// Flat snake_case event matching integration-contracts schema for ticket.created.
/// Serialized via System.Text.Json with SnakeCaseLower naming policy.
/// </summary>
public record TicketCreatedEvent
{
    public string EventType { get; init; } = "ticket.created";
    public string Timestamp { get; init; } = DateTime.UtcNow.ToString("o");
    public string Source { get; init; } = "ticket-masala";
    public string TicketId { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Priority { get; init; } = "medium";
    public string CreatedAt { get; init; } = DateTime.UtcNow.ToString("o");
    public string EventId { get; init; } = Guid.NewGuid().ToString();
}
