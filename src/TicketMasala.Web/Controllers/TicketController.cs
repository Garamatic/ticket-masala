using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Facades;
using TicketMasala.Web.Modules.Tickets;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Controllers;

/// <summary>
/// Main controller for ticket CRUD operations (Create, Read, Update, Detail).
/// Uses ITicketModule deep module for all operations.
/// </summary>
[Authorize]
public class TicketController : Controller
{
    private readonly ITicketModule _ticketModule;
    private readonly ILogger<TicketController> _logger;

    public TicketController(
        ITicketModule ticketModule,
        ILogger<TicketController> logger)
    {
        _ticketModule = ticketModule;
        _logger = logger;
    }

    public async Task<IActionResult> Index(TicketSearchViewModel searchModel)
    {
        var result = await _ticketModule.SearchForUiAsync(searchModel, User);

        ViewBag.SavedFilters = result.SavedFilters;
        ViewBag.IsCustomer = User?.IsInRole(Constants.RoleCustomer) ?? false;
        return View("~/Views/TicketSearch/Index.cshtml", result);
    }

    #region Detail

    public async Task<IActionResult> Detail(Guid? id)
    {
        if (id == null)
            return NotFound();

        TicketDetailsViewModel? viewModel;
        TicketDetailContext context;

        try
        {
            (viewModel, context) = await _ticketModule.GetDetailPageAsync(id.Value, User);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        if (viewModel == null)
            return NotFound();

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
            var summary = await _ticketModule.GenerateAiSummaryAsync(ticketId);
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
        var context = await _ticketModule.GetCreateContextAsync(projectGuid, User);

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
        string? description,
        string? customerId,
        string? responsibleId,
        Guid? projectGuid,
        DateTime? completionTarget,
        string? domainId,
        string? workItemTypeCode)
    {
        try
        {
            return await CreateInternal(description, customerId, responsibleId, projectGuid, completionTarget, domainId, workItemTypeCode);
        }
        catch (NullReferenceException)
        {
            return BadRequest("Request could not be processed due to missing context");
        }
    }

    private async Task<IActionResult> CreateInternal(
        string? description,
        string? customerId,
        string? responsibleId,
        Guid? projectGuid,
        DateTime? completionTarget,
        string? domainId,
        string? workItemTypeCode)
    {
        // Guard against null HttpContext (can happen in some test scenarios)
        if (HttpContext?.Request == null)
        {
            return BadRequest("Invalid request context");
        }

        // Ensure User is not null
        var user = User ?? new ClaimsPrincipal(new ClaimsIdentity());

        // Input validation for form UX
        if (string.IsNullOrWhiteSpace(description))
            ModelState.AddModelError("description", "Description is required");

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        // Auto-assign customer for customer users
        if (user.IsInRole(Constants.RoleCustomer) && !string.IsNullOrEmpty(userId))
        {
            customerId = userId;
        }

        if (string.IsNullOrWhiteSpace(customerId) && !user.IsInRole(Constants.RoleCustomer))
        {
            ModelState.AddModelError("customerId", "Customer is required");
        }

        if (ModelState.IsValid)
        {
            // Build custom fields from form (safely handle null form)
            var customFields = Request.Form?.Count > 0
                ? Request.Form
                    .Where(x => x.Key.StartsWith("customFields["))
                    .ToDictionary(
                        x => x.Key.Replace("customFields[", "").Replace("]", ""),
                        x => x.Value.ToString())
                : new Dictionary<string, string>();

            var command = new CreateTicketCommand(
                description ?? string.Empty,
                customerId ?? string.Empty,
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
        var context = await _ticketModule.GetCreateReloadContextAsync(projectGuid, user);
        ViewBag.Employees = context?.Employees ?? new List<SelectListItem>();
        ViewBag.Projects = context?.Projects ?? new List<SelectListItem>();
        ViewBag.Customers = context?.Customers ?? new List<SelectListItem>();
        ViewBag.IsCustomer = context?.IsCustomer ?? false;
        ViewBag.DomainId = context?.DomainId ?? "IT";
        ViewBag.EntityLabels = context?.EntityLabels;
        ViewBag.WorkItemTypes = context?.WorkItemTypes;
        ViewBag.CustomFields = context?.CustomFields;

        return View();
    }

    #endregion

    #region Edit

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
            return NotFound();

        TicketEditContext? context;
        try
        {
            context = await _ticketModule.GetEditContextAsync(id.Value, User);
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
            var currentUser = User ?? new ClaimsPrincipal(new ClaimsIdentity());
            var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var roles = currentUser.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

            // Build custom fields from form
            var customFields = Request.Form?.Count > 0
                ? Request.Form
                    .Where(x => x.Key.StartsWith("customFields["))
                    .ToDictionary(
                        x => x.Key.Replace("customFields[", "").Replace("]", ""),
                        x => x.Value.ToString())
                : new Dictionary<string, string>();

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
        var reloadUser = User ?? new ClaimsPrincipal(new ClaimsIdentity());
        var listsContext = await _ticketModule.GetCreateReloadContextAsync(viewModel.ProjectGuid, reloadUser);
        viewModel.ResponsibleUsers = listsContext.Employees?.ToList() ?? new List<SelectListItem>();
        viewModel.CustomerList = listsContext.Customers?.ToList() ?? new List<SelectListItem>();
        viewModel.ProjectList = listsContext.Projects?.ToList() ?? new List<SelectListItem>();

        var context = await _ticketModule.GetEditReloadContextAsync(id, reloadUser);

        if (context?.ValidStatuses != null)
        {
            ViewBag.ValidStatuses = context.ValidStatuses;
        }

        ViewBag.DomainId = context?.DomainId ?? "IT";
        ViewBag.EntityLabels = context?.EntityLabels;
        ViewBag.CustomFields = context?.CustomFields;
        ViewBag.CustomFieldValues = new Dictionary<string, object>();

        return View(viewModel);
    }

    #endregion
}
