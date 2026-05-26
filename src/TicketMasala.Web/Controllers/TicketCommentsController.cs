using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;

namespace TicketMasala.Web.Controllers;

[Authorize]
public class TicketCommentsController : Controller
{
    private readonly ITicketLifecycle _ticketLifecycle;
    private readonly ILogger<TicketCommentsController> _logger;

    public TicketCommentsController(ITicketLifecycle ticketLifecycle, ILogger<TicketCommentsController> logger)
    {
        _ticketLifecycle = ticketLifecycle;
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
                TempData["Error"] = result.ErrorMessage ?? "Failed to add comment.";
                return RedirectToAction("Detail", "Ticket", new { id });
            }
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
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var result = await _ticketLifecycle.ExecuteAsync(
                new RequestReviewCommand(id),
                new TicketContext(userId));

            if (!result.Success)
            {
                _logger.LogWarning("RequestReview failed for ticket {TicketId}: {Error}", id, result.ErrorMessage);
                TempData["Error"] = result.ErrorMessage ?? "Failed to request review.";
            }
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
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var result = await _ticketLifecycle.ExecuteAsync(
                new SubmitReviewCommand(id, score, feedback, approve),
                new TicketContext(userId));

            if (!result.Success)
            {
                _logger.LogWarning("SubmitReview failed for ticket {TicketId}: {Error}", id, result.ErrorMessage);
                TempData["Error"] = result.ErrorMessage ?? "Failed to submit review.";
            }
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
