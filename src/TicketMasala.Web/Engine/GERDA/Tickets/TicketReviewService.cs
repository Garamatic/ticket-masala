using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Enums;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// Service responsible for ticket quality review operations.
/// Handles requesting and submitting quality reviews.
/// </summary>
[Obsolete("Use ITicketLifecycle and command records instead. This interface will be removed in a future release.", false)]
public interface ITicketReviewService
{
    /// <summary>
    /// Requests a quality review for a ticket.
    /// </summary>
    /// <param name="ticketId">The ticket to review</param>
    /// <param name="requesterId">The user requesting the review</param>
    /// <returns>True if request succeeded, false if ticket not found</returns>
    Task<bool> RequestReviewAsync(Guid ticketId, string requesterId);

    /// <summary>
    /// Submits a quality review for a ticket.
    /// </summary>
    /// <param name="ticketId">The ticket being reviewed</param>
    /// <param name="score">The quality score (typically 1-10)</param>
    /// <param name="feedback">Review feedback/comments</param>
    /// <param name="approved">Whether the ticket is approved or rejected</param>
    /// <param name="reviewerId">The user performing the review</param>
    /// <returns>True if review submitted, false if ticket not found</returns>
    Task<bool> SubmitReviewAsync(Guid ticketId, int score, string feedback, bool approved, string reviewerId);
}

/// <summary>
/// Implementation of ticket quality review operations.
/// </summary>
internal class TicketReviewService : ITicketReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ISystemClock _clock;
    private readonly ILogger<TicketReviewService> _logger;

    public TicketReviewService(
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ISystemClock clock,
        ILogger<TicketReviewService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> RequestReviewAsync(Guid ticketId, string requesterId)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId, includeRelations: false);
        if (ticket == null)
        {
            _logger.LogWarning("Cannot request review: ticket {TicketId} not found", ticketId);
            return false;
        }

        ticket.SetReviewStatus(ReviewStatus.Pending);

        // Queue ticket update (not yet committed)
        await _unitOfWork.Tickets.UpdateAsync(ticket);

        // Audit trail (also queued)
        await _auditService.LogActionAsync(ticketId, "ReviewRequested", requesterId);

        // Commit all changes in a single transaction
        await _unitOfWork.CommitAsync();

        _logger.LogInformation(
            "Quality review requested for ticket {TicketId} by {RequesterId}",
            ticketId,
            requesterId);

        return true;
    }

    public async Task<bool> SubmitReviewAsync(Guid ticketId, int score, string feedback, bool approved, string reviewerId)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId, includeRelations: false);
        if (ticket == null)
        {
            _logger.LogWarning("Cannot submit review: ticket {TicketId} not found", ticketId);
            return false;
        }

        // Update ticket review status
        ticket.SetReviewStatus(approved ? ReviewStatus.Approved : ReviewStatus.Rejected);

        // Create review record
        var review = new QualityReview
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            ReviewerId = reviewerId,
            Score = score,
            Comments = feedback,
            CreatedAt = _clock.UtcNow,
            IsApproved = approved
        };

        // Queue both changes (not yet committed)
        await _unitOfWork.AddQualityReviewAsync(review);
        await _unitOfWork.Tickets.UpdateAsync(ticket);

        // Audit trail (also queued)
        await _auditService.LogActionAsync(
            ticketId,
            approved ? "ReviewApproved" : "ReviewRejected",
            reviewerId,
            "QualityReview",
            null,
            feedback);

        // Commit all changes in a single transaction
        await _unitOfWork.CommitAsync();

        _logger.LogInformation(
            "Quality review submitted for ticket {TicketId} by {ReviewerId}. Approved: {Approved}, Score: {Score}",
            ticketId,
            reviewerId,
            approved,
            score);

        return true;
    }
}
