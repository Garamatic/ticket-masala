using TicketMasala.Domain.Common;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.Projects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Engine.Core;

namespace TicketMasala.Web.Controllers;

[Authorize]
public class TicketSearchController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly ISavedFilterService _savedFilterService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TicketSearchController> _logger;

    public TicketSearchController(
        ITicketService ticketService,
        ISavedFilterService savedFilterService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TicketSearchController> logger)
    {
        _ticketService = ticketService;
        _savedFilterService = savedFilterService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(TicketSearchViewModel searchModel)
    {
        try
        {
            if (searchModel == null) searchModel = new TicketSearchViewModel();

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isCustomer = User.IsInRole(Constants.RoleCustomer);

            if (isCustomer && !string.IsNullOrEmpty(userId))
            {
                searchModel.CustomerId = userId;
            }

            var result = await _ticketService.SearchTicketsAsync(searchModel);

            result.Customers = await _ticketService.GetCustomerSelectListAsync();
            result.Employees = await _ticketService.GetEmployeeSelectListAsync();
            result.Projects = await _ticketService.GetProjectSelectListAsync();

            if (!string.IsNullOrEmpty(userId))
            {
                ViewBag.SavedFilters = await _savedFilterService.GetFiltersForUserAsync(userId);
            }

            ViewBag.IsCustomer = isCustomer;

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
