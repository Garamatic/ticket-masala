using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities; // ApplicationUser, Employee

namespace TicketMasala.Web.Observers;

/// <summary>
/// Observer interface for ticket lifecycle events.
/// Implements Observer pattern for event-driven architecture.
/// Observers are notified when tickets are created, assigned, or completed.
/// </summary>
public interface ITicketObserver
{
    /// <summary>
    /// Called when a new ticket is created
    /// </summary>
    Task OnTicketCreatedAsync(Ticket ticket);

    /// <summary>
    /// Called when a ticket is assigned to an employee
    /// </summary>
    Task OnTicketAssignedAsync(Ticket ticket, Employee assignee);

    /// <summary>
    /// Called when a ticket is completed
    /// </summary>
    Task OnTicketCompletedAsync(Ticket ticket);

    /// <summary>
    /// Called when a ticket is updated (general)
    /// </summary>
    Task OnTicketUpdatedAsync(Ticket ticket);

    /// <summary>
    /// Called when a comment is added to a ticket
    /// </summary>
    Task OnTicketCommentedAsync(TicketComment comment);

}
