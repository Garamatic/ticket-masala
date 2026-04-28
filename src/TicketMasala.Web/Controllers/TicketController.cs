using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Modules.Tickets;
using TicketMasala.Web.Orchestrators;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Controllers;

/// <summary>
/// Main controller for ticket CRUD operations (Create, Read, Update, Detail).
/// Refactored to use ITicketModule deep module for business operations.
/// </summary>
[Authorize]
public class TicketController : Controller
{
    private readonly ITicketModule _ticketModule;
    private readonly ITicketOrchestrator _orchestrator; // Kept for UI context operations
    private readonly ILogger<TicketController> _logger;

    public TicketController(
        ITicketModule ticketModule,
        ITicketOrchestrator orchestrator,
        ILogger<TicketController> logger)
    {
        _ticketModule = ticketModule;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<IActionResult> Index(TicketSearchViewModel searchModel)
    {
        // Use orchestrator for search (complex query with full DTOs)
        // TODO: Migrate search to TicketModule once module supports full TicketSearchResultDto
        var result = await _orchestrator.SearchTicketsAsync(searchModel, User);

        ViewBag.SavedFilters = result.SavedFilters;
        ViewBag.IsCustomer = User.IsInRole(Constants.RoleCustomer);
        return View("~/Views/TicketSearch/Index.cshtml", result);
    }

    #region Detail

    public async Task<IActionResult> Detail(Guid? id)
    {
        if (id == null)
            return NotFound();

        // Keep orchestrator for full view model and UI context
        TicketDetailsViewModel? viewModel;
        try
        {
            viewModel = await _orchestrator.GetTicketDetailsAsync(id.Value, User);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        if (viewModel == null)
            return NotFound();

        var context = await _orchestrator.GetTicketDetailContextAsync(viewModel);

        ViewBag.DomainId = context.DomainId;
        ViewBag.EntityLabels = context.EntityLabels;
        ViewBag.CustomFields = context.CustomFields;
        ViewBag.WorkItemTypeCode = context.WorkItemTypeCode;
        ViewBag.CustomFieldValues = context.CustomFieldValues;

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> GenerateAiSummary(Guid ticketId)
    {
        try
        {
            // Keep using orchestrator for AI features until those are also modularized
            var summary = await _orchestrator.GenerateAiSummaryAsync(ticketId);
            return Json(new { success = true, summary });
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Failed to generate summary: {ex.Message}" });
        }
    }

    #endregion

    #region Create

    [HttpGet]
    public async Task<IActionResult> Create(Guid? projectGuid = null)
    {
        // Keep orchestrator for UI context
        var context = await _orchestrator.GetCreateContextAsync(projectGuid, User);

        ViewBag.Employees = context.Employees;
        ViewBag.Projects = context.Projects;
        ViewBag.Customers = context.Customers;
        ViewBag.PreselectedProjectId = context.PreselectedProjectId;
        ViewBag.PreselectedCustomerId = context.PreselectedCustomerId;
        ViewBag.IsCustomer = context.IsCustomer;
        ViewBag.DomainId = context.DomainId;
        ViewBag.EntityLabels = context.EntityLabels;
        ViewBag.WorkItemTypes = context.WorkItemTypes;
        ViewBag.CustomFields = context.CustomFields;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string description,
        string customerId,
        string? responsibleId,
        Guid? projectGuid,
        DateTime? completionTarget,
        string? domainId,
        string? workItemTypeCode)
    {
        // Input validation for form UX
        if (string.IsNullOrWhiteSpace(description))
            ModelState.AddModelError("description", "Description is required");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        // Auto-assign customer for customer users
        if (User.IsInRole(Constants.RoleCustomer) && !string.IsNullOrEmpty(userId))
        {
            customerId = userId;
        }

        if (string.IsNullOrWhiteSpace(customerId) && !User.IsInRole(Constants.RoleCustomer))
        {
            ModelState.AddModelError("customerId", "Customer is required");
        }

        if (ModelState.IsValid)
        {
            // Build custom fields from form
            var customFields = Request.Form
                .Where(x => x.Key.StartsWith("customFields["))
                .ToDictionary(
                    x => x.Key.Replace("customFields[", "").Replace("]", ""),
                    x => x.Value.ToString());

            var command = new CreateTicketCommand(
                description,
                customerId,
                responsibleId,
                projectGuid,
                completionTarget,
                domainId,
                workItemTypeCode,
                customFields,
                userId);

            var result = await _ticketModule.CreateAsync(command);

            if (result.IsSuccess)
            {
                TempData["Success"] = "Ticket created successfully! GERDA AI has processed the ticket.";
                return RedirectToAction("Index", "TicketSearch");
            }

            TempData["Warning"] = result.ErrorMessage;
        }

        // Reload context on failure
        var context = await _orchestrator.GetCreateContextAsync(projectGuid, User);
        ViewBag.Employees = context.Employees;
        ViewBag.Projects = context.Projects;
        ViewBag.Customers = context.Customers;
        ViewBag.IsCustomer = context.IsCustomer;
        ViewBag.DomainId = context.DomainId;
        ViewBag.EntityLabels = context.EntityLabels;
        ViewBag.WorkItemTypes = context.WorkItemTypes;
        ViewBag.CustomFields = context.CustomFields;

        return View();
    }

    #endregion

    #region Edit

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
            return NotFound();

        // Keep orchestrator for UI context and view model construction
        Facades.TicketEditContext? context;
        try
        {
            context = await _orchestrator.GetEditContextAsync(id.Value, User);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Detail", new { id = id.Value });
        }

        if (context == null)
            return NotFound();

        if (context.ValidStatuses != null)
        {
            ViewBag.ValidStatuses = context.ValidStatuses;
        }

        ViewBag.DomainId = context.DomainId;
        ViewBag.EntityLabels = context.EntityLabels;
        ViewBag.CustomFields = context.CustomFields;
        ViewBag.WorkItemTypeCode = context.WorkItemTypeCode;
        ViewBag.CustomFieldValues = context.CustomFieldValues;

        return View(context.ViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditTicketViewModel viewModel)
    {
        if (id != viewModel.Guid)
            return NotFound();

        if (ModelState.IsValid)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

            // Build custom fields from form
            var customFields = Request.Form
                .Where(x => x.Key.StartsWith("customFields["))
                .ToDictionary(
                    x => x.Key.Replace("customFields[", "").Replace("]", ""),
                    x => x.Value.ToString());

            var command = new UpdateTicketCommand(
                id,
                viewModel.Description,
                viewModel.TicketStatus.ToString(),
                viewModel.CompletionTarget,
                viewModel.CustomerId ?? string.Empty,
                viewModel.ProjectGuid,
                customFields,
                userId,
                roles);

            var result = await _ticketModule.UpdateAsync(command);

            if (result.IsSuccess)
            {
                return RedirectToAction(nameof(Detail), new { id = id });
            }

            ModelState.AddModelError("", result.ErrorMessage ?? "Failed to update ticket.");
        }

        // Reload context
        var listsContext = await _orchestrator.GetCreateContextAsync(viewModel.ProjectGuid, User);
        viewModel.ResponsibleUsers = listsContext.Employees?.ToList() ?? new List<SelectListItem>();
        viewModel.CustomerList = listsContext.Customers?.ToList() ?? new List<SelectListItem>();
        viewModel.ProjectList = listsContext.Projects?.ToList() ?? new List<SelectListItem>();

        var context = await _orchestrator.GetEditReloadContextAsync(id, User);

        if (context.ValidStatuses != null)
        {
            ViewBag.ValidStatuses = context.ValidStatuses;
        }

        ViewBag.DomainId = context.DomainId;
        ViewBag.EntityLabels = context.EntityLabels;
        ViewBag.CustomFields = context.CustomFields;
        ViewBag.CustomFieldValues = new Dictionary<string, object>();

        return View(viewModel);
    }

    #endregion
}
