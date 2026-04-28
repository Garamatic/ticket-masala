using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Modules.Tickets;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Controllers;

/// <summary>
/// Controller for ticket search operations.
/// Uses ITicketModule deep module for all operations.
/// </summary>
[Authorize]
public class TicketSearchController : Controller
{
    private readonly ITicketModule _ticketModule;
    private readonly ISavedFilterService _savedFilterService;

    public TicketSearchController(
        ITicketModule ticketModule,
        ISavedFilterService savedFilterService)
    {
        _ticketModule = ticketModule;
        _savedFilterService = savedFilterService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(TicketSearchViewModel searchModel)
    {
        var result = await _ticketModule.SearchForUiAsync(searchModel, User);

        ViewBag.SavedFilters = result.SavedFilters;
        ViewBag.IsCustomer = User.IsInRole(Constants.RoleCustomer);

        return View(result);
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

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _savedFilterService.SaveFilterAsync(userId, name, searchModel);

        TempData["Success"] = "Filter saved successfully.";
        return RedirectToAction(nameof(Index), searchModel);
    }

    [HttpGet]
    public async Task<IActionResult> LoadFilter(Guid id)
    {
        var filter = await _savedFilterService.GetFilterAsync(id);
        if (filter == null)
            return NotFound();

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
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Forbid();

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
