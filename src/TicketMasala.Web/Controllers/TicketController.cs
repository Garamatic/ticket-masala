using TicketMasala.Web;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Web.ViewModels.Projects;
using TicketMasala.Web.ViewModels.Customers;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Engine.Ingestion.Background;
using TicketMasala.Web.Engine.GERDA;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Web.Engine.Compiler;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Data;
using TicketMasala.Web.AI;

namespace TicketMasala.Web.Controllers;

/// <summary>
/// Main controller for ticket CRUD operations (Create, Read, Update, Detail).
/// </summary>
[Authorize]
public class TicketController : Controller
{
    private readonly IGerdaService _gerdaService;
    private readonly ITicketWorkflowService _ticketWorkflowService;
    private readonly ITicketReadService _ticketReadService;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly IDomainConfigurationService _domainConfig;
    private readonly IProjectReadService _projectReadService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRuleEngineService _ruleEngine;
    private readonly IOpenAiService _openAiService;
    private readonly Facades.ITicketContextFacade _ticketContextFacade;
    private readonly ILogger<TicketController> _logger;

    public TicketController(
        IGerdaService gerdaService,
        ITicketWorkflowService ticketWorkflowService,
        ITicketReadService ticketReadService,
        IAuditService auditService,
        INotificationService notificationService,
        IDomainConfigurationService domainConfig,
        IProjectReadService projectReadService,
        IHttpContextAccessor httpContextAccessor,
        IRuleEngineService ruleEngine,
        IOpenAiService openAiService,
        Facades.ITicketContextFacade ticketContextFacade,
        ILogger<TicketController> logger)
    {
        _gerdaService = gerdaService;
        _ticketWorkflowService = ticketWorkflowService;
        _ticketReadService = ticketReadService;
        _auditService = auditService;
        _notificationService = notificationService;
        _domainConfig = domainConfig;
        _projectReadService = projectReadService;
        _httpContextAccessor = httpContextAccessor;
        _ruleEngine = ruleEngine;
        _openAiService = openAiService;
        _ticketContextFacade = ticketContextFacade;
        _logger = logger;
    }

    public async Task<IActionResult> Index(TicketSearchViewModel searchModel)
    {
        if (searchModel == null) searchModel = new TicketSearchViewModel();

        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = User.IsInRole(Constants.RoleCustomer);
        _logger.LogInformation("DEBUG: Index Access - UserId: {UserId}, IsCustomer: {IsCustomer}, QueryCustomerId: {QueryCustId}, QueryStatus: {Status}",
            userId, isCustomer, searchModel.CustomerId, searchModel.Status);
        if (isCustomer && !string.IsNullOrEmpty(userId)) searchModel.CustomerId = userId;

        var result = await _ticketReadService.SearchTicketsAsync(searchModel);
        result.Customers = await _ticketReadService.GetCustomerSelectListAsync();
        result.Employees = await _ticketReadService.GetEmployeeSelectListAsync();
        result.Projects = await _ticketReadService.GetProjectSelectListAsync();

        if (!string.IsNullOrEmpty(userId))
        {
            var savedFilterService = HttpContext.RequestServices.GetService<ISavedFilterService>();
            if (savedFilterService != null)
                ViewBag.SavedFilters = await savedFilterService.GetFiltersForUserAsync(userId);
        }
        ViewBag.IsCustomer = isCustomer;
        return View("~/Views/TicketSearch/Index.cshtml", result);
    }

    #region Detail

    public async Task<IActionResult> Detail(Guid? id)
    {
        if (id == null) return NotFound();

        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = User.IsInRole(Constants.RoleCustomer);

        TicketDetailsViewModel? viewModel;
        try
        {
            viewModel = await _ticketContextFacade.GetTicketDetailsAsync(id.Value, userId, isCustomer);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }

        if (viewModel == null) return NotFound();

        var context = await _ticketContextFacade.GetTicketDetailContextAsync(viewModel);
        
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
        var ticket = await _ticketReadService.GetTicketDetailsAsync(ticketId);
        if (ticket == null) return NotFound();

        var query = $"Title: {ticket.Description} (Created: {ticket.CreationDate})\n\n" +
                $"Status: {ticket.TicketStatus}\n\n" +
                $"Discussion:\n" +
                string.Join("\n", ticket.Comments.OrderBy(c => c.CreatedAt).Select(c => $"- {c.Author?.Name ?? c.Author?.UserName ?? "Unknown"} ({c.CreatedAt}): {c.Body}"));

        try
        {
            var summary = await _openAiService.GetResponseAsync(OpenAIPrompts.Summary, query);
            return Json(new { success = true, summary });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI summary for ticket {TicketId}", ticketId);
            return Json(new { success = false, message = $"Failed to generate summary: {ex.Message}" });
        }
    }

    #endregion

    #region Create

    [HttpGet]
    public async Task<IActionResult> Create(Guid? projectGuid = null)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = User.IsInRole(Constants.RoleCustomer);

        var context = await _ticketContextFacade.GetCreateContextAsync(isCustomer, userId, projectGuid);

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
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = User?.IsInRole(Constants.RoleCustomer) ?? false;

        if (isCustomer && !string.IsNullOrEmpty(userId))
        {
            customerId = userId;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            ModelState.AddModelError("description", "Description is required");
        }

        if (string.IsNullOrWhiteSpace(customerId))
        {
            ModelState.AddModelError("customerId", "Customer is required");
        }

        if (!ModelState.IsValid)
        {
            if (!isCustomer)
            {
                ViewBag.Customers = await _ticketReadService.GetCustomerSelectListAsync();
            }
            ViewBag.Employees = await _ticketReadService.GetEmployeeSelectListAsync();
            ViewBag.Projects = await _ticketReadService.GetProjectSelectListAsync();
            ViewBag.IsCustomer = isCustomer;

            var reloadDomain = _domainConfig.GetDefaultDomainId();
            ViewBag.DomainId = reloadDomain;
            ViewBag.EntityLabels = _domainConfig.GetEntityLabels(reloadDomain);
            ViewBag.WorkItemTypes = _domainConfig.GetWorkItemTypes(reloadDomain).ToList();
            ViewBag.CustomFields = _domainConfig.GetCustomFields(reloadDomain).ToList();

            return View();
        }

        try
        {
            var ticket = await _ticketWorkflowService.CreateTicketAsync(description, customerId, responsibleId, projectGuid, completionTarget);

            ticket.DomainId = domainId ?? _domainConfig.GetDefaultDomainId();
            ticket.WorkItemTypeCode = workItemTypeCode;

            var formDictionary = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
            ticket.CustomFieldsJson = _ticketReadService.ParseCustomFields(ticket.DomainId, formDictionary);

            await _ticketWorkflowService.UpdateTicketAsync(ticket);

            _logger.LogInformation("Processing ticket {TicketGuid} with GERDA AI (Domain: {DomainId}, Type: {WorkItemTypeCode})",
                ticket.Guid, ticket.DomainId, ticket.WorkItemTypeCode);
            await _gerdaService.ProcessTicketAsync(ticket.Guid);

            var entityLabel = _domainConfig.GetEntityLabels(ticket.DomainId).WorkItem;
            TempData["Success"] = $"{entityLabel} created successfully! GERDA AI has processed the {entityLabel.ToLower()} (estimated effort, priority, and tags assigned).";
            _logger.LogInformation("GERDA processing completed for ticket {TicketGuid}", ticket.Guid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating or processing ticket");
            TempData["Warning"] = "Creation encountered an error. Please try again.";
        }

        return RedirectToAction("Index", "TicketSearch");
    }

    #endregion

    #region Edit

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();

        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = User.IsInRole(Constants.RoleCustomer);

        Facades.TicketEditContext? context;
        try
        {
            context = await _ticketContextFacade.GetEditContextAsync(id.Value, userId, isCustomer);
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

        if (context == null) return NotFound();

        // Facade can't easily populate ValidStatuses without User principal, handled here or passed to Facade.
        // In the updated Facade I omitted it, so I need to restore it here or update Facade to take ClaimsPrincipal.
        // Let's populate ValidStatuses here for now to be safe, or assume I should have passed user.
        // I'll populate it here since I have the viewmodel.
        // Wait, context.ViewModel is EditTicketViewModel, not Ticket. I need Ticket entity for RuleEngine.
        // The facade loaded the ticket but didn't expose it. 
        // This suggests GetEditContextAsync should probably handle ValidStatuses if I pass User.
        
        // RE-READING FACADE: I didn't implement ValidStatuses in GetEditContextAsync.
        // I should probably just do it here for now to save a facade change, OR update facade.
        // But I need the Ticket entity for RuleEngine.GetValidNextStates(ticket, User).
        // The facade has the ticket. It should do it.
        
        // Let's update the Facade to accept ClaimsPrincipal instead of userId string.
        // But I already wrote the facade. 
        // I will use what I have. I can fetch the ticket again or...
        // Actually, the facade returned ViewModel, not Ticket.
        // This is a small design flaw in my facade implementation. 
        
        // QUICK FIX: I will just re-fetch the ticket here for RuleEngine. It's a small overhead (cached context likely).
        // OR better: Update Facade to take ClaimsPrincipal.
        // Since I'm editing Controller, I can't easily update Facade in same step.
        // I will stick to Controller changes.
        
        var ticket = await _ticketReadService.GetTicketForEditAsync(id.Value); // This is cached/fast usually
        if (ticket != null)
        {
            var validStates = _ruleEngine.GetValidNextStates(ticket, User);
            var allowedStatuses = validStates.Union(new[] { ticket.TicketStatus }).Distinct().ToList();
            ViewBag.ValidStatuses = new SelectList(allowedStatuses);
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
        if (id != viewModel.Guid) return NotFound();

        if (ModelState.IsValid)
        {
            var ticketToUpdate = await _ticketReadService.GetTicketForEditAsync(id);
            if (ticketToUpdate == null) return NotFound();

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isCustomer = User.IsInRole(Constants.RoleCustomer);

            if (isCustomer)
            {
                if (ticketToUpdate.CustomerId != userId) return Forbid();

                if (ticketToUpdate.TicketStatus != Status.Pending && ticketToUpdate.TicketStatus != Status.Assigned)
                {
                    TempData["ErrorMessage"] = "You can only edit tickets that are in Pending or Assigned status.";
                    return RedirectToAction("Detail", new { id = ticketToUpdate.Guid });
                }
            }

            ticketToUpdate.Description = viewModel.Description;
            ticketToUpdate.TicketStatus = viewModel.TicketStatus;
            ticketToUpdate.CompletionTarget = viewModel.CompletionTarget;
            ticketToUpdate.CustomerId = viewModel.CustomerId;
            ticketToUpdate.ProjectGuid = viewModel.ProjectGuid;

            var domainId = ticketToUpdate.DomainId ?? _domainConfig.GetDefaultDomainId();
            var formDictionary = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
            ticketToUpdate.CustomFieldsJson = _ticketReadService.ParseCustomFields(domainId, formDictionary);

            try
            {
                var success = await _ticketWorkflowService.UpdateTicketAsync(ticketToUpdate);
                if (success)
                {
                    return RedirectToAction(nameof(Detail), new { id = ticketToUpdate.Guid });
                }
                else
                {
                    ModelState.AddModelError("", "Failed to update ticket. Please try again.");
                }
            }
            catch (DomainRuleException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }
        }

        viewModel.ResponsibleUsers = await _ticketReadService.GetAllUsersSelectListAsync();
        viewModel.CustomerList = (await _ticketReadService.GetCustomerSelectListAsync()).ToList();
        viewModel.ProjectList = (await _ticketReadService.GetProjectSelectListAsync()).ToList();

        var context = await _ticketContextFacade.GetEditReloadContextAsync(id, User);
        
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
