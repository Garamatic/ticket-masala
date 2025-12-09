using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TicketMasala.Web.Data;
using TicketMasala.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace TicketMasala.Web.Controllers;

[Authorize]
public class TicketCommentsController : Controller
{
    private readonly MasalaDbContext _context;
    private readonly ILogger<TicketCommentsController> _logger;
    
    public TicketCommentsController(MasalaDbContext context, ILogger<TicketCommentsController> logger)
    {
        _context = context;
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
        {
            return Unauthorized();
        }

        var comment = new TicketComment
        {
            Id = Guid.NewGuid(),
            TicketId = id,
            Body = commentBody,
            IsInternal = isInternal,
            AuthorId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.TicketComments.Add(comment);
        await _context.SaveChangesAsync();

       TempData["Success"] = "Comment added successfully.";
        return RedirectToAction("Detail", "Ticket", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestReview(Guid id)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket == null)
        {
            return NotFound();
        }

        ticket.ReviewStatus = ReviewStatus.Pending;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Review requested successfully.";
        return RedirectToAction("Detail", "Ticket", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Employee")]
    public async Task<IActionResult> SubmitReview(Guid id, int score, string feedback, bool approve)
    {
        if (string.IsNullOrWhiteSpace(feedback))
        {
            TempData["Error"] = "Feedback is required.";
            return RedirectToAction("Detail", "Ticket", new { id });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket == null)
        {
            return NotFound();
        }

        var review = new QualityReview
        {
            Id = Guid.NewGuid(),
            TicketId = id,
            Ticket = ticket,
            ReviewerId = userId,
            Score = score,
            Comments = feedback,
            CreatedAt = DateTime.UtcNow
        };

        _context.QualityReviews.Add(review);
        
        ticket.ReviewStatus = approve ? ReviewStatus.Approved : ReviewStatus.Rejected;
        await _context.SaveChangesAsync();

        TempData["Success"] = approve ? "Review approved." : "Review rejected.";
        return RedirectToAction("Detail", "Ticket", new { id });
    }
}
