using TicketMasala.Web.Models;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Engine.Ingestion.Background;

namespace TicketMasala.Web.Observers;

/// <summary>
/// Sends notifications when comments are added to tickets.
/// Notifies relevant parties: ticket responsible, customer, and mentioned users.
/// </summary>
public class NotificationCommentObserver : ICommentObserver
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationCommentObserver> _logger;

    public NotificationCommentObserver(
        INotificationService notificationService,
        ILogger<NotificationCommentObserver> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task OnCommentAddedAsync(TicketComment comment)
    {
        if (comment.Ticket == null)
        {
            _logger.LogWarning("Comment {CommentId} has no associated ticket", comment.Id);
            return;
        }

        var ticket = comment.Ticket;
        var ticketShortId = ticket.Guid.ToString()[..8];
        var commentPreview = comment.Body.Length > 50
            ? comment.Body[..50] + "..."
            : comment.Body;

        // Don't notify the author of their own comment
        var authorId = comment.AuthorId;

        // Notify ticket responsible (if not the author)
        if (!string.IsNullOrEmpty(ticket.ResponsibleId) && ticket.ResponsibleId != authorId)
        {
            await _notificationService.NotifyUserAsync(
                ticket.ResponsibleId,
                $"New {(comment.IsInternal ? "internal note" : "comment")} on ticket #{ticketShortId}",
                $"/Ticket/Detail/{ticket.Guid}",
                comment.IsInternal ? "Warning" : "Info");

            _logger.LogDebug("Notified responsible {UserId} of comment on ticket {TicketId}",
                ticket.ResponsibleId, ticket.Guid);
        }

        // Notify customer (only for non-internal comments)
        var customerId = ticket.CreatorGuid.ToString();
        if (!comment.IsInternal &&
            !string.IsNullOrEmpty(customerId) &&
            customerId != authorId)
        {
            await _notificationService.NotifyUserAsync(
                customerId,
                $"New reply on your ticket #{ticketShortId}",
                $"/Ticket/Detail/{ticket.Guid}",
                "Info");

            _logger.LogDebug("Notified customer {UserId} of comment on ticket {TicketId}",
                customerId, ticket.Guid);
        }

        _logger.LogInformation("Comment notifications sent for ticket {TicketId}", ticket.Guid);
    }

    public async Task OnCommentEditedAsync(TicketComment comment)
    {
        // Typically no notification needed for edits
        _logger.LogDebug("Comment {CommentId} was edited", comment.Id);
        await Task.CompletedTask;
    }

    public async Task OnCommentDeletedAsync(TicketComment comment)
    {
        // Typically no notification needed for deletions
        _logger.LogDebug("Comment {CommentId} was deleted", comment.Id);
        await Task.CompletedTask;
    }
}

/// <summary>
/// Logs comment events for auditing.
/// </summary>
public class LoggingCommentObserver : ICommentObserver
{
    private readonly ILogger<LoggingCommentObserver> _logger;

    public LoggingCommentObserver(ILogger<LoggingCommentObserver> logger)
    {
        _logger = logger;
    }

    public Task OnCommentAddedAsync(TicketComment comment)
    {
        _logger.LogInformation(
            "Comment added: TicketId={TicketId}, Author={AuthorId}, IsInternal={IsInternal}",
            comment.TicketId, comment.AuthorId, comment.IsInternal);
        return Task.CompletedTask;
    }

    public Task OnCommentEditedAsync(TicketComment comment)
    {
        _logger.LogInformation("Comment edited: CommentId={CommentId}", comment.Id);
        return Task.CompletedTask;
    }

    public Task OnCommentDeletedAsync(TicketComment comment)
    {
        _logger.LogInformation("Comment deleted: CommentId={CommentId}", comment.Id);
        return Task.CompletedTask;
    }

}
