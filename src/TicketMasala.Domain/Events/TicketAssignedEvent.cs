namespace TicketMasala.Domain.Events;

/// <summary>
/// Raised when a ticket is assigned to an employee.
/// </summary>
public record TicketAssignedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    /// <summary>
    /// The unique identifier of the ticket.
    /// </summary>
    public Guid TicketGuid { get; }

    /// <summary>
    /// The ID of the employee the ticket is now assigned to.
    /// </summary>
    public string NewResponsibleId { get; }

    /// <summary>
    /// The ID of the previous responsible employee, if any.
    /// </summary>
    public string? OldResponsibleId { get; }

    /// <summary>
    /// The ID of the user who performed the assignment.
    /// </summary>
    public string AssignedByUserId { get; }

    public TicketAssignedEvent(Guid ticketGuid, string newResponsibleId, string? oldResponsibleId, string assignedByUserId)
    {
        TicketGuid = ticketGuid;
        NewResponsibleId = newResponsibleId;
        OldResponsibleId = oldResponsibleId;
        AssignedByUserId = assignedByUserId;
    }
}
