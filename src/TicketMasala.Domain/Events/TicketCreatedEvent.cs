namespace TicketMasala.Domain.Events;

/// <summary>
/// Raised when a new ticket is created.
/// </summary>
public record TicketCreatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    /// <summary>
    /// The unique identifier of the created ticket.
    /// </summary>
    public Guid TicketGuid { get; }

    /// <summary>
    /// The ID of the customer who created the ticket.
    /// </summary>
    public string CustomerId { get; }

    /// <summary>
    /// The ID of the domain this ticket belongs to.
    /// </summary>
    public string DomainId { get; }

    public TicketCreatedEvent(Guid ticketGuid, string customerId, string domainId)
    {
        TicketGuid = ticketGuid;
        CustomerId = customerId;
        DomainId = domainId;
    }
}
