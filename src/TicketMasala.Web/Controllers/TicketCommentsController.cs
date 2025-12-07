using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TicketMasala.Web.Engine.GERDA.Tickets;

namespace TicketMasala.Web.Controllers;

[Authorize]
public class TicketCommentsController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TicketCommentsController(
        ITicketService ticketService,
        IHttpContextAccessor httpContextAccessor)
    {
        _ticketService = ticketService;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Guid id, string commentBody, bool isInternal)
    {
        if (!string.IsNullOrWhiteSpace(commentBody))
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            try 
            {
                await _ticketService.AddCommentAsync(id, commentBody, isInternal, currentUserId);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
        }

        return RedirectToAction("Detail", "Ticket", new { id = id });
    }
}
