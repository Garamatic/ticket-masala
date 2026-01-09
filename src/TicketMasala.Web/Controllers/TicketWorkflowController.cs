using TicketMasala.Domain.Common;
using TicketMasala.Web.Engine.GERDA.Tickets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace TicketMasala.Web.Controllers;

[Authorize]
public class TicketWorkflowController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TicketWorkflowController> _logger;

    public TicketWorkflowController(
        ITicketService ticketService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TicketWorkflowController> logger)
    {
        _ticketService = ticketService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignToRecommended(Guid ticketGuid, string agentId)
    {
        var success = await _ticketService.AssignTicketAsync(ticketGuid, agentId);

        if (!success)
        {
            TempData["Error"] = "Failed to assign ticket. Please try again.";
            return RedirectToAction("Index", "TicketSearch");
        }

        var agent = await _ticketService.GetEmployeeByIdAsync(agentId);
        TempData["Success"] = $"Ticket successfully assigned to {agent?.FirstName} {agent?.LastName}!";
        return RedirectToAction("Detail", "Ticket", new { id = ticketGuid });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(Guid id, string commentBody, bool isInternal)
    {
        if (string.IsNullOrWhiteSpace(commentBody))
        {
            if (Request.Headers.ContainsKey("HX-Request"))
            {
                return BadRequest("Comment body is required");
            }

            TempData["Error"] = "Comment cannot be empty";
            return RedirectToAction("Detail", "Ticket", new { id });
        }

        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        try
        {
            await _ticketService.AddCommentAsync(id, commentBody, isInternal, userId);

            if (Request.Headers.ContainsKey("HX-Request"))
            {
                var ticketDetails = await _ticketService.GetTicketDetailsAsync(id);
                if (ticketDetails != null)
                {
                    return PartialView("_CommentListPartial", ticketDetails.Comments);
                }
                return StatusCode(500, "Ticket not found");
            }

            TempData["Success"] = "Comment added successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to ticket {TicketId}", id);
            if (Request.Headers.ContainsKey("HX-Request"))
            {
                return StatusCode(500, "Error adding comment");
            }
            TempData["Error"] = "Failed to add comment";
        }

        return RedirectToAction("Detail", "Ticket", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestReview(Guid id)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _ticketService.RequestReviewAsync(id, userId);

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            var ticketDetails = await _ticketService.GetTicketDetailsAsync(id);
            return PartialView("_QualityReviewPartial", ticketDetails);
        }

        return RedirectToAction("Detail", "Ticket", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReview(Guid id, int score, string feedback, bool approve)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _ticketService.SubmitReviewAsync(id, score, feedback, approve, userId);

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            var ticketDetails = await _ticketService.GetTicketDetailsAsync(id);
            return PartialView("_QualityReviewPartial", ticketDetails);
        }

        return RedirectToAction("Detail", "Ticket", new { id });
    }
}
