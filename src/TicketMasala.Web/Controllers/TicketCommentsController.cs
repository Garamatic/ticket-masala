using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TicketMasala.Web.Engine.GERDA.Tickets;
using System.Security.Claims;

namespace TicketMasala.Web.Controllers;

[Authorize]
public class TicketCommentsController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly ILogger<TicketCommentsController> _logger;

    public TicketCommentsController(ITicketService ticketService, ILogger<TicketCommentsController> logger)
    {
        _ticketService = ticketService;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(Guid id, string commentBody, bool isInternal = false)
    {
        if (string.IsNullOrWhiteSpace(commentBody))
        {
            TempData["Error"] = "Comment cannot be empty.";
            return RedirectToAction("Detail", "Ticket", new { id });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        try
        {
            await _ticketService.AddCommentAsync(id, commentBody, isInternal, userId);
            TempData["Success"] = "Comment added successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment.");
            TempData["Error"] = "Failed to add comment.";
        }

        return RedirectToAction("Detail", "Ticket", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestReview(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        try
        {
            await _ticketService.RequestReviewAsync(id, userId);
            TempData["Success"] = "Review requested successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting review.");
            TempData["Error"] = "Failed to request review.";
        }

        return RedirectToAction("Detail", "Ticket", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<IActionResult> SubmitReview(Guid id, int score, string feedback, bool approve)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        try
        {
            await _ticketService.SubmitReviewAsync(id, score, feedback, approve, userId);
            TempData["Success"] = approve ? "Review approved." : "Review rejected.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting review.");
            TempData["Error"] = "Failed to submit review.";
        }

        return RedirectToAction("Detail", "Ticket", new { id });
    }
}
