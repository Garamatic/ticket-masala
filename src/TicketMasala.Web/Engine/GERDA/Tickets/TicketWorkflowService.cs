using Microsoft.AspNetCore.Http;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Abstractions;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// [OBSOLETE] This service is being replaced by specialized services and ITicketModule.
/// 
/// All methods now delegate to:
/// - ITicketCreationService (CreateTicketAsync)
/// - ITicketUpdateService (UpdateTicketAsync)
/// - ITicketAssignmentFacade (AssignTicketAsync, AssignTicketWithProjectAsync)
/// - ITicketResolutionService (ResolveTicketAsync)
/// - ITicketCommentService (AddCommentAsync)
/// - ITicketReviewService (RequestReviewAsync, SubmitReviewAsync)
/// - ITicketTimeLoggingService (LogTimeAsync)
/// 
/// New code should use ITicketModule or the specific service interfaces directly.
/// This wrapper remains for backward compatibility during migration period.
/// 
/// Migration Target: Remove this class after all callers migrate (Target: 30 days)
/// </summary>
[Obsolete("Use ITicketModule or specific service interfaces (ITicketCreationService, ITicketUpdateService, etc.) instead. This service will be removed in a future release.", false)]
public class TicketWorkflowService : ITicketWorkflowService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITicketResolutionService _resolutionService;
    private readonly ITicketCommentService _commentService;
    private readonly ITicketReviewService _reviewService;
    private readonly ITicketTimeLoggingService _timeLoggingService;
    private readonly ITicketCreationService _creationService;
    private readonly ITicketUpdateService _updateService;
    private readonly ITicketAssignmentFacade _assignmentFacade;

    public TicketWorkflowService(
        IHttpContextAccessor httpContextAccessor,
        ITicketResolutionService resolutionService,
        ITicketCommentService commentService,
        ITicketReviewService reviewService,
        ITicketTimeLoggingService timeLoggingService,
        ITicketCreationService creationService,
        ITicketUpdateService updateService,
        ITicketAssignmentFacade assignmentFacade)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _resolutionService = resolutionService ?? throw new ArgumentNullException(nameof(resolutionService));
        _commentService = commentService ?? throw new ArgumentNullException(nameof(commentService));
        _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService));
        _timeLoggingService = timeLoggingService ?? throw new ArgumentNullException(nameof(timeLoggingService));
        _creationService = creationService ?? throw new ArgumentNullException(nameof(creationService));
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
        _assignmentFacade = assignmentFacade ?? throw new ArgumentNullException(nameof(assignmentFacade));
    }

    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? null;
    }

    public Task<Ticket> CreateTicketAsync(
        string description,
        string customerId,
        string? responsibleId,
        Guid? projectGuid,
        DateTime? completionTarget)
    {
        return _creationService.CreateAsync(description, customerId, responsibleId, projectGuid, completionTarget, GetCurrentUserId());
    }

    public Task<bool> UpdateTicketAsync(Ticket ticket)
    {
        return _updateService.UpdateAsync(ticket, GetCurrentUserId());
    }

    public Task<bool> AssignTicketAsync(Guid ticketGuid, string agentId)
    {
        return _assignmentFacade.AssignAsync(ticketGuid, agentId, GetCurrentUserId());
    }

    public Task<bool> AssignTicketWithProjectAsync(Guid ticketGuid, string? agentId, Guid? projectGuid)
    {
        return _assignmentFacade.AssignWithProjectAsync(ticketGuid, agentId, projectGuid, GetCurrentUserId());
    }

    public Task<TicketComment> AddCommentAsync(Guid ticketId, string body, bool isInternal, string authorId)
    {
        return _commentService.AddCommentAsync(ticketId, body, isInternal, authorId);
    }

    public Task<bool> RequestReviewAsync(Guid ticketId, string requesterId)
    {
        return _reviewService.RequestReviewAsync(ticketId, requesterId);
    }

    public Task<bool> SubmitReviewAsync(Guid ticketId, int score, string feedback, bool approved, string reviewerId)
    {
        return _reviewService.SubmitReviewAsync(ticketId, score, feedback, approved, reviewerId);
    }

    public Task<TimeLog> LogTimeAsync(Guid ticketId, string userId, double hours, DateTime date, string description)
    {
        return _timeLoggingService.LogTimeAsync(ticketId, userId, hours, date, description);
    }

    public Task<bool> ResolveTicketAsync(
        Guid ticketGuid,
        string resolutionNotes,
        decimal? billableAmount,
        string resolvedByUserId)
    {
        return _resolutionService.ResolveAsync(ticketGuid, resolutionNotes, billableAmount, resolvedByUserId);
    }
}
