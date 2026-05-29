namespace TicketMasala.Web.Messaging.Events;

/// <summary>
/// Flat snake_case event matching integration-contracts schema for ticket.assigned.
/// Serialized via System.Text.Json with SnakeCaseLower naming policy.
/// </summary>
public record TicketAssignedEvent
{
    public string EventType { get; init; } = "ticket.assigned";
    public string Timestamp { get; init; } = DateTime.UtcNow.ToString("o");
    public string Source { get; init; } = "ticket-masala";
    public string TicketId { get; init; } = string.Empty;
    public string AssignedTo { get; init; } = string.Empty;
    public string AssignedBy { get; init; } = string.Empty;
    public string AssignedAt { get; init; } = DateTime.UtcNow.ToString("o");
    public string EventId { get; init; } = Guid.NewGuid().ToString();
}
