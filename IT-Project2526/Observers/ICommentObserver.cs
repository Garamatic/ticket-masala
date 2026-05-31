using IT_Project2526.Models;

namespace IT_Project2526.Observers;

/// <summary>
/// Observer interface for comment events.
/// Centralizes notification logic for ticket comments.
/// </summary>
public interface ICommentObserver
{
    /// <summary>
    /// Called when a new comment is added to a ticket.
    /// </summary>
    Task OnCommentAddedAsync(TicketComment comment);

    /// <summary>
    /// Called when a comment is edited.
    /// </summary>
    Task OnCommentEditedAsync(TicketComment comment);

    /// <summary>
    /// Called when a comment is deleted.
    /// </summary>
    Task OnCommentDeletedAsync(TicketComment comment);
}
