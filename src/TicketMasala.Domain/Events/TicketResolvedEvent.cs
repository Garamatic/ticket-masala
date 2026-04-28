namespace TicketMasala.Domain.Events;

/// <summary>
/// Raised when a ticket is resolved (marked as completed).
/// This event triggers downstream billing workflows.
/// </summary>
public record TicketResolvedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    /// <summary>
    /// The unique identifier of the resolved ticket.
    /// </summary>
    public Guid TicketGuid { get; }

    /// <summary>
    /// The ID of the customer who owns the ticket.
    /// </summary>
    public string CustomerId { get; }

    /// <summary>
    /// The billable amount for this ticket (null if not billable).
    /// </summary>
    public decimal? BillableAmount { get; }

    /// <summary>
    /// Notes about how the ticket was resolved.
    /// </summary>
    public string? ResolutionNotes { get; }

    /// <summary>
    /// When the ticket was resolved.
    /// </summary>
    public DateTime ResolvedAt { get; }

    /// <summary>
    /// The ID of the user who resolved the ticket.
    /// </summary>
    public string ResolvedByUserId { get; }

    public TicketResolvedEvent(
        Guid ticketGuid,
        string customerId,
        decimal? billableAmount,
        string? resolutionNotes,
        DateTime resolvedAt,
        string resolvedByUserId)
    {
        TicketGuid = ticketGuid;
        CustomerId = customerId;
        BillableAmount = billableAmount;
        ResolutionNotes = resolutionNotes;
        ResolvedAt = resolvedAt;
        ResolvedByUserId = resolvedByUserId;
    }
}
