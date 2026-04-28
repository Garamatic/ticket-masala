using TicketMasala.Domain.Entities;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Observers;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// Service responsible for ticket comment operations.
/// Handles adding comments, notifications, and audit logging.
/// </summary>
public interface ITicketCommentService
{
    /// <summary>
    /// Adds a comment to a ticket.
    /// </summary>
    /// <param name="ticketId">The ticket to comment on</param>
    /// <param name="body">The comment text</param>
    /// <param name="isInternal">Whether this is an internal-only note</param>
    /// <param name="authorId">The user adding the comment</param>
    /// <returns>The created comment</returns>
    /// <exception cref="ArgumentException">Thrown if ticket not found</exception>
    Task<TicketComment> AddCommentAsync(Guid ticketId, string body, bool isInternal, string authorId);
}

/// <summary>
/// Implementation of ticket comment operations.
/// </summary>
internal class TicketCommentService : ITicketCommentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly IEnumerable<ITicketObserver> _ticketObservers;
    private readonly IEnumerable<ICommentObserver> _commentObservers;
    private readonly ISystemClock _clock;
    private readonly ILogger<TicketCommentService> _logger;

    public TicketCommentService(
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        IEnumerable<ITicketObserver> ticketObservers,
        IEnumerable<ICommentObserver> commentObservers,
        ISystemClock clock,
        ILogger<TicketCommentService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _ticketObservers = ticketObservers ?? throw new ArgumentNullException(nameof(ticketObservers));
        _commentObservers = commentObservers ?? throw new ArgumentNullException(nameof(commentObservers));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TicketComment> AddCommentAsync(Guid ticketId, string body, bool isInternal, string authorId)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId, includeRelations: true);
        if (ticket == null)
        {
            throw new ArgumentException("Ticket not found", nameof(ticketId));
        }

        var comment = new TicketComment
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Body = body,
            IsInternal = isInternal,
            CreatedAt = _clock.UtcNow,
            AuthorId = authorId,
            Ticket = ticket
        };

        // Queue comment add (not yet committed)
        await _unitOfWork.AddCommentAsync(comment);

        // Audit trail (also queued)
        await _auditService.LogActionAsync(
            ticketId,
            "Commented",
            authorId,
            "Comment",
            null,
            isInternal ? "Internal Note" : "Public Reply");

        // Commit all changes in a single transaction
        await _unitOfWork.CommitAsync();

        // Notify observers (after commit to ensure data is persisted)
        await NotifyObserversAsync(ticket, comment);

        _logger.LogInformation(
            "Comment added to ticket {TicketId} by {AuthorId}. Internal: {IsInternal}",
            ticketId,
            authorId,
            isInternal);

        return comment;
    }

    private async Task NotifyObserversAsync(Ticket ticket, TicketComment comment)
    {
        // Notify ticket observers
        foreach (var observer in _ticketObservers)
        {
            try
            {
                await observer.OnTicketCommentedAsync(comment);
                await observer.OnTicketUpdatedAsync(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "TicketObserver {ObserverType} failed during comment notification",
                    observer.GetType().Name);
            }
        }

        // Notify comment-specific observers
        foreach (var observer in _commentObservers)
        {
            try
            {
                await observer.OnCommentAddedAsync(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "CommentObserver {ObserverType} failed during comment notification",
                    observer.GetType().Name);
            }
        }
    }
}
