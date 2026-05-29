namespace TicketMasala.Web.Messaging.Events;

public record TicketAssignedEvent
{
    public string EventType { get; init; } = "ticket.assigned";
    public string Timestamp { get; init; } = DateTime.UtcNow.ToString("o");
    public string Source { get; init; } = "ticket-masala";
    public string TicketId { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string AssignedTo { get; init; } = string.Empty;
    public string AssignedBy { get; init; } = string.Empty;
    public string AssignedAt { get; init; } = DateTime.UtcNow.ToString("o");
    public string EventId { get; init; } = Guid.NewGuid().ToString();
}
