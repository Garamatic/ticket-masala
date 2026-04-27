using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Web;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.AI;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.Compiler;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Engine.Ingestion.Background;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.Orchestrators;
using TicketMasala.Web.ViewModels.Customers;
using TicketMasala.Web.ViewModels.Projects;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Controllers;

/// <summary>
/// Main controller for ticket CRUD operations (Create, Read, Update, Detail).
/// </summary>
[Authorize]
public class TicketController : Controller
{
    private readonly ITicketOrchestrator _orchestrator;
    private readonly ILogger<TicketController> _logger;

    public TicketController(
        ITicketOrchestrator orchestrator,
        ILogger<TicketController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<IActionResult> Index(TicketSearchViewModel searchModel)
    {
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
        // Input validation for form UX (Orchestrator has defensive validation as well)
        if (string.IsNullOrWhiteSpace(description))
            ModelState.AddModelError("description", "Description is required");
        if (string.IsNullOrWhiteSpace(customerId))
        {
            // Only require customerId for non-customer users (customers are auto-assigned)
            if (!User.IsInRole(Constants.RoleCustomer))
            {
                ModelState.AddModelError("customerId", "Customer is required");
            }
        }

        if (ModelState.IsValid)
        {
            var result = await _orchestrator.CreateTicketAsync(
                description, customerId, responsibleId, projectGuid, completionTarget, domainId, workItemTypeCode, Request.Form, User);

            if (result.IsSuccess)
            {
                TempData["Success"] = result.SuccessMessage;
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

        // We might need to preserve the user's selected domain if possible, but for simplicity we reload default or let view handle it.
        // Context has DomainId from config/defaults.
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

        // Pass ValidStatuses if available (Orchestrator should populate it)
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
            var result = await _orchestrator.UpdateTicketAsync(id, viewModel, Request.Form, User);

            if (result.IsSuccess)
            {
                return RedirectToAction(nameof(Detail), new { id = id });
            }

            ModelState.AddModelError("", result.ErrorMessage ?? "Failed to update ticket.");
        }

        // Reload context
        viewModel.ResponsibleUsers = (await _orchestrator.GetCreateContextAsync(null, User)).Employees?.ToList() ?? new List<SelectListItem>();
        // Note: We might want a specialized Reload method in Orchestrator that returns ViewModel ready lists.
        // reusing GetCreateContextAsync for lists is a bit hacky but works for employees/projects/customers.
        // But GetEditReloadContextAsync is better.

        var context = await _orchestrator.GetEditReloadContextAsync(id, User);

        if (context.ValidStatuses != null)
        {
            ViewBag.ValidStatuses = context.ValidStatuses;
        }

        // We also need to repopulate lists in ViewModel if they are null.
        // Orchestrator.UpdateTicketAsync doesn't return a ViewModel.
        // We have to manually repopulate.
        // Since GetEditReloadContextAsync returns context, does it have lists?
        // TicketEditContext definition shows it DOES NOT have lists for ViewModel properties (ResponsibleUsers, etc).
        // It has ValidStatuses.
        // So we need to fetch lists.
        // I'll add a helper in Orchestrator or just use GetCreateContextAsync logic here.
        // Actually, let's just fetch them via GetCreateContextAsync for simplicity, as I did above.
        var listsContext = await _orchestrator.GetCreateContextAsync(viewModel.ProjectGuid, User);
        viewModel.ResponsibleUsers = listsContext.Employees?.ToList() ?? new List<SelectListItem>();
        viewModel.CustomerList = listsContext.Customers?.ToList() ?? new List<SelectListItem>();
        viewModel.ProjectList = listsContext.Projects?.ToList() ?? new List<SelectListItem>();

        ViewBag.DomainId = context.DomainId;
        ViewBag.EntityLabels = context.EntityLabels;
        ViewBag.CustomFields = context.CustomFields;
        ViewBag.CustomFieldValues = new Dictionary<string, object>(); // Reset or preserve?

        return View(viewModel);
    }

    #endregion
}
