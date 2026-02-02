using TicketMasala.Domain.Common;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.Projects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Orchestrators;

namespace TicketMasala.Web.Controllers;

[Authorize]
public class TicketSearchController : Controller
{
    private readonly ITicketOrchestrator _orchestrator;
    private readonly ISavedFilterService _savedFilterService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TicketSearchController> _logger;

    public TicketSearchController(
        ITicketOrchestrator orchestrator,
        ISavedFilterService savedFilterService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TicketSearchController> logger)
    {
        _orchestrator = orchestrator;
        _savedFilterService = savedFilterService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(TicketSearchViewModel searchModel)
    {
        try
        {
            var result = await _orchestrator.SearchTicketsAsync(searchModel, User);
            
            // Orchestrator populates SavedFilters in the result now
            ViewBag.SavedFilters = result.SavedFilters;
            ViewBag.IsCustomer = User.IsInRole(Constants.RoleCustomer);

            return View(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tickets");
            return StatusCode(500);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveFilter(string name, TicketSearchViewModel searchModel)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Filter name is required.";
            return RedirectToAction(nameof(Index), searchModel);
        }

        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _savedFilterService.SaveFilterAsync(userId, name, searchModel);

        TempData["Success"] = "Filter saved successfully.";
        return RedirectToAction(nameof(Index), searchModel);
    }

    [HttpGet]
    public async Task<IActionResult> LoadFilter(Guid id)
    {
        var filter = await _savedFilterService.GetFilterAsync(id);
        if (filter == null) return NotFound();

        var searchModel = new TicketSearchViewModel
        {
            SearchTerm = filter.SearchTerm,
            Status = filter.Status,
            TicketType = filter.TicketType,
            ProjectId = filter.ProjectId,
            AssignedToId = filter.AssignedToId,
            CustomerId = filter.CustomerId,
            IsOverdue = filter.IsOverdue ?? false,
            IsDueSoon = filter.IsDueSoon ?? false
        };

        return RedirectToAction(nameof(Index), searchModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFilter(Guid id)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Forbid();

        try
        {
            await _savedFilterService.DeleteFilterAsync(id, userId);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        TempData["Success"] = "Filter deleted.";
        return RedirectToAction(nameof(Index));
    }
}
