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

namespace TicketMasala.Web.Controllers;

/// <summary>
/// Main controller for ticket CRUD operations (Create, Read, Update, Detail).
/// </summary>
[Authorize]
public class TicketController : Controller
{
    private readonly IGerdaService _gerdaService;
    private readonly ITicketService _ticketService;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly IDomainConfigurationService _domainConfig;
    private readonly IProjectService _projectService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRuleEngineService _ruleEngine;
    private readonly ILogger<TicketController> _logger;

    public TicketController(
        IGerdaService gerdaService,
        ITicketService ticketService,
        IAuditService auditService,
        INotificationService notificationService,
        IDomainConfigurationService domainConfig,
        IProjectService projectService,
        IHttpContextAccessor httpContextAccessor,
        IRuleEngineService ruleEngine,
        ILogger<TicketController> logger)
    {
        _gerdaService = gerdaService;
        _ticketService = ticketService;
        _auditService = auditService;
        _notificationService = notificationService;
        _domainConfig = domainConfig;
        _projectService = projectService;
        _httpContextAccessor = httpContextAccessor;
        _ruleEngine = ruleEngine;
        _logger = logger;
    }

    #region Detail

    public async Task<IActionResult> Detail(Guid? id)
    {
        if (id == null) return NotFound();

        var viewModel = await _ticketService.GetTicketDetailsAsync(id.Value);
        if (viewModel == null) return NotFound();

        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = User.IsInRole(Constants.RoleCustomer);

        if (isCustomer && viewModel.CustomerId != userId)
        {
            return Forbid();
        }

        // Get recommended agent for unassigned tickets
        if (string.IsNullOrWhiteSpace(viewModel.ResponsibleId))
        {
            try
            {
                var dispatchingService = HttpContext.RequestServices.GetService<Engine.GERDA.Dispatching.IDispatchingService>();
                if (dispatchingService != null)
                {
                    var recommendations = await dispatchingService.GetTopRecommendedAgentsAsync(id.Value, 1);
                    if (recommendations != null && recommendations.Any())
                    {
                        var topRecommendation = recommendations.First();
                        var agent = await _ticketService.GetEmployeeByIdAsync(topRecommendation.AgentId);
                        if (agent != null)
                        {
                            var currentWorkload = await _ticketService.GetEmployeeCurrentWorkloadAsync(agent.Id);
                            viewModel.RecommendedAgent = new RecommendedAgentInfo
                            {
                                AgentId = agent.Id,
                                AgentName = $"{agent.FirstName} {agent.LastName}",
                                AffinityScore = topRecommendation.Score,
                                CurrentWorkload = currentWorkload,
                                MaxCapacity = agent.MaxCapacityPoints
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get recommended agent for ticket {TicketGuid}", id.Value);
            }
        }

        var domainId = viewModel.DomainId ?? _domainConfig.GetDefaultDomainId();
        ViewBag.DomainId = domainId;
        ViewBag.EntityLabels = _domainConfig.GetEntityLabels(domainId);
        ViewBag.CustomFields = _domainConfig.GetCustomFields(domainId).ToList();
        ViewBag.WorkItemTypeCode = viewModel.WorkItemTypeCode;

        if (!string.IsNullOrEmpty(viewModel.CustomFieldsJson))
        {
            try
            {
                ViewBag.CustomFieldValues = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(viewModel.CustomFieldsJson);
            }
            catch
            {
                ViewBag.CustomFieldValues = new Dictionary<string, object>();
            }
        }
        else
        {
            ViewBag.CustomFieldValues = new Dictionary<string, object>();
        }

        return View(viewModel);
    }

    #endregion

    #region Create

    [HttpGet]
    public async Task<IActionResult> Create(Guid? projectGuid = null)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = User.IsInRole(Constants.RoleCustomer);

        ViewBag.Employees = await _ticketService.GetEmployeeSelectListAsync();
        ViewBag.Projects = await _ticketService.GetProjectSelectListAsync();

        string? preselectedCustomerId = null;

        if (projectGuid.HasValue)
        {
            var project = await _projectService.GetProjectDetailsAsync(projectGuid.Value);
            if (project != null && project.ProjectDetails != null)
            {
                ViewBag.PreselectedProjectId = project.ProjectDetails.Guid;
                if (!string.IsNullOrEmpty(project.ProjectDetails.CustomerId))
                {
                    preselectedCustomerId = project.ProjectDetails.CustomerId;
                }
            }
        }

        if (!isCustomer)
        {
            ViewBag.Customers = await _ticketService.GetCustomerSelectListAsync();
            if (preselectedCustomerId != null)
            {
                ViewBag.PreselectedCustomerId = preselectedCustomerId;
            }
        }
        else
        {
            ViewBag.PreselectedCustomerId = userId;
        }

        ViewBag.IsCustomer = isCustomer;

        var defaultDomain = _domainConfig.GetDefaultDomainId();
        ViewBag.DomainId = defaultDomain;
        ViewBag.EntityLabels = _domainConfig.GetEntityLabels(defaultDomain);
        ViewBag.WorkItemTypes = _domainConfig.GetWorkItemTypes(defaultDomain).ToList();
        ViewBag.CustomFields = _domainConfig.GetCustomFields(defaultDomain).ToList();

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
                ViewBag.Customers = await _ticketService.GetCustomerSelectListAsync();
            }
            ViewBag.Employees = await _ticketService.GetEmployeeSelectListAsync();
            ViewBag.Projects = await _ticketService.GetProjectSelectListAsync();
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
            var ticket = await _ticketService.CreateTicketAsync(description, customerId, responsibleId, projectGuid, completionTarget);

            ticket.DomainId = domainId ?? _domainConfig.GetDefaultDomainId();
            ticket.WorkItemTypeCode = workItemTypeCode;

            var formDictionary = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
            ticket.CustomFieldsJson = _ticketService.ParseCustomFields(ticket.DomainId, formDictionary);

            await _ticketService.UpdateTicketAsync(ticket);

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

        var ticket = await _ticketService.GetTicketForEditAsync(id.Value);
        if (ticket == null) return NotFound();

        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = User.IsInRole(Constants.RoleCustomer);

        if (isCustomer)
        {
            if (ticket.CustomerId != userId) return Forbid();

            if (ticket.TicketStatus != Status.Pending && ticket.TicketStatus != Status.Assigned)
            {
                TempData["ErrorMessage"] = "You can only edit tickets that are in Pending or Assigned status.";
                return RedirectToAction("Detail", new { id = ticket.Guid });
            }
        }

        var responsibleUsers = await _ticketService.GetAllUsersSelectListAsync();

        var viewModel = new EditTicketViewModel
        {
            Guid = ticket.Guid,
            Description = ticket.Description,
            TicketStatus = ticket.TicketStatus,
            CompletionTarget = ticket.CompletionTarget,
            ResponsibleUserId = ticket.Responsible?.Id,
            CustomerId = ticket.CustomerId,
            ProjectGuid = ticket.ProjectGuid,
            ResponsibleUsers = responsibleUsers,
            CustomerList = (await _ticketService.GetCustomerSelectListAsync()).ToList(),
            ProjectList = (await _ticketService.GetProjectSelectListAsync()).ToList()
        };

        var domainId = ticket.DomainId ?? _domainConfig.GetDefaultDomainId();
        ViewBag.DomainId = domainId;
        ViewBag.EntityLabels = _domainConfig.GetEntityLabels(domainId);
        ViewBag.CustomFields = _domainConfig.GetCustomFields(domainId).ToList();
        ViewBag.WorkItemTypeCode = ticket.WorkItemTypeCode;

        if (!string.IsNullOrEmpty(ticket.CustomFieldsJson))
        {
            try { ViewBag.CustomFieldValues = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(ticket.CustomFieldsJson); }
            catch { ViewBag.CustomFieldValues = new Dictionary<string, object>(); }
        }
        else
        {
            ViewBag.CustomFieldValues = new Dictionary<string, object>();
        }

        var validStates = _ruleEngine.GetValidNextStates(ticket, User);
        var allowedStatuses = validStates.Union(new[] { ticket.TicketStatus }).Distinct().ToList();
        ViewBag.ValidStatuses = new SelectList(allowedStatuses);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditTicketViewModel viewModel)
    {
        if (id != viewModel.Guid) return NotFound();

        if (ModelState.IsValid)
        {
            var ticketToUpdate = await _ticketService.GetTicketForEditAsync(id);
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
            ticketToUpdate.CustomFieldsJson = _ticketService.ParseCustomFields(domainId, formDictionary);

            try
            {
                var success = await _ticketService.UpdateTicketAsync(ticketToUpdate);
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

        viewModel.ResponsibleUsers = await _ticketService.GetAllUsersSelectListAsync();
        viewModel.CustomerList = (await _ticketService.GetCustomerSelectListAsync()).ToList();
        viewModel.ProjectList = (await _ticketService.GetProjectSelectListAsync()).ToList();

        var reloadTicket = await _ticketService.GetTicketForEditAsync(id);
        if (reloadTicket != null)
        {
            var validStates = _ruleEngine.GetValidNextStates(reloadTicket, User);
            var allowedStatuses = validStates.Union(new[] { reloadTicket.TicketStatus }).Distinct().ToList();
            ViewBag.ValidStatuses = new SelectList(allowedStatuses);
        }

        var reloadDomainId = _domainConfig.GetDefaultDomainId();
        ViewBag.DomainId = reloadDomainId;
        ViewBag.EntityLabels = _domainConfig.GetEntityLabels(reloadDomainId);
        ViewBag.CustomFields = _domainConfig.GetCustomFields(reloadDomainId).ToList();
        ViewBag.CustomFieldValues = new Dictionary<string, object>();

        return View(viewModel);
    }

    #endregion
}
