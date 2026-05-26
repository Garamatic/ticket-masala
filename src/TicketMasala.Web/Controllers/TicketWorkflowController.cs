using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;

namespace TicketMasala.Web.Controllers;

[Authorize]
public class TicketWorkflowController : Controller
{
    private readonly ITicketLifecycle _ticketLifecycle;
    private readonly ITicketReadService _ticketReadService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TicketWorkflowController> _logger;

    public TicketWorkflowController(
        ITicketLifecycle ticketLifecycle,
        ITicketReadService ticketReadService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TicketWorkflowController> logger)
    {
        _ticketLifecycle = ticketLifecycle;
        _ticketReadService = ticketReadService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private bool IsHtmxRequest => Request.Headers.ContainsKey("HX-Request");

    private string? GetCurrentUserId()
        => _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    private TicketContext CreateContext()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("User ID not found in claims.");
        return new TicketContext(userId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignToRecommended(Guid ticketGuid, string agentId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _ticketLifecycle.ExecuteAsync(
            new AssignTicketCommand(ticketGuid, agentId),
            new TicketContext(userId));

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage ?? "Failed to assign ticket. Please try again.";
            return RedirectToAction("Index", "TicketSearch");
        }

        var agent = await _ticketReadService.GetEmployeeByIdAsync(agentId);
        TempData["Success"] = $"Ticket successfully assigned to {agent?.FirstName} {agent?.LastName}!";
        return RedirectToAction("Detail", "Ticket", new { id = ticketGuid });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(Guid id, string commentBody, bool isInternal)
    {
        if (string.IsNullOrWhiteSpace(commentBody))
        {
            if (IsHtmxRequest)
                return BadRequest("Comment body is required");

            TempData["Error"] = "Comment cannot be empty";
            return RedirectToAction("Detail", "Ticket", new { id });
        }

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var result = await _ticketLifecycle.ExecuteAsync(
                new AddCommentCommand(id, commentBody, isInternal),
                new TicketContext(userId));

            if (!result.Success)
            {
                _logger.LogWarning("AddComment failed for ticket {TicketId}: {Error}", id, result.ErrorMessage);
                if (IsHtmxRequest)
                    return StatusCode(500, result.ErrorMessage ?? "Error adding comment");
                TempData["Error"] = result.ErrorMessage ?? "Failed to add comment";
                return RedirectToAction("Detail", "Ticket", new { id });
            }

            if (IsHtmxRequest)
            {
                var ticketDetails = await _ticketReadService.GetTicketDetailsAsync(id);
                return ticketDetails != null
                    ? PartialView("_CommentListPartial", ticketDetails.Comments)
                    : StatusCode(500, "Ticket not found");
            }

            TempData["Success"] = "Comment added successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to ticket {TicketId}", id);
            if (IsHtmxRequest)
                return StatusCode(500, "Error adding comment");
            TempData["Error"] = "Failed to add comment";
        }

        return RedirectToAction("Detail", "Ticket", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestReview(Guid id)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _ticketLifecycle.ExecuteAsync(
            new RequestReviewCommand(id),
            new TicketContext(userId));

        if (!result.Success)
        {
            _logger.LogWarning("RequestReview failed for ticket {TicketId}: {Error}", id, result.ErrorMessage);
            if (IsHtmxRequest)
                return StatusCode(500, result.ErrorMessage ?? "Failed to request review");
            TempData["Error"] = result.ErrorMessage ?? "Failed to request review";
            return RedirectToAction("Detail", "Ticket", new { id });
        }

        if (IsHtmxRequest)
        {
            var ticketDetails = await _ticketReadService.GetTicketDetailsAsync(id);
            return PartialView("_QualityReviewPartial", ticketDetails);
        }

        return RedirectToAction("Detail", "Ticket", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReview(Guid id, int score, string feedback, bool approve)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _ticketLifecycle.ExecuteAsync(
            new SubmitReviewCommand(id, score, feedback, approve),
            new TicketContext(userId));

        if (!result.Success)
        {
            _logger.LogWarning("SubmitReview failed for ticket {TicketId}: {Error}", id, result.ErrorMessage);
            if (IsHtmxRequest)
                return StatusCode(500, result.ErrorMessage ?? "Failed to submit review");
            TempData["Error"] = result.ErrorMessage ?? "Failed to submit review";
            return RedirectToAction("Detail", "Ticket", new { id });
        }

        if (IsHtmxRequest)
        {
            var ticketDetails = await _ticketReadService.GetTicketDetailsAsync(id);
            return PartialView("_QualityReviewPartial", ticketDetails);
        }

        return RedirectToAction("Detail", "Ticket", new { id });
    }
}
