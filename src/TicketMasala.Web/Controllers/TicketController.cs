using TicketMasala.Web;
using TicketMasala.Web.Models;
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
using TicketMasala.Web.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Data;

namespace TicketMasala.Web.Controllers;
[Authorize] // All authenticated users can access tickets
public class TicketController : Controller
{
    private readonly IGerdaService? _gerdaService;
    private readonly ITicketService _ticketService;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly IDomainConfigurationService _domainConfig;
    private readonly MasalaDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRuleEngineService _ruleEngine;
    private readonly ILogger<TicketController> _logger;

    public TicketController(
        ITicketService ticketService,
        IAuditService auditService,
        INotificationService notificationService,
        IDomainConfigurationService domainConfig,
        MasalaDbContext context,
        IHttpContextAccessor httpContextAccessor,
        IRuleEngineService ruleEngine,
        ILogger<TicketController> logger,
        IGerdaService? gerdaService = null)
    {
        _gerdaService = gerdaService;
        _ticketService = ticketService;
        _auditService = auditService;
        _notificationService = notificationService;
        _domainConfig = domainConfig;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _ruleEngine = ruleEngine;
        _logger = logger;
    }

    public async Task<IActionResult> Index(TicketSearchViewModel searchModel)
    {
        try
        {
            // Initialize defaults if needed
            if (searchModel == null) searchModel = new TicketSearchViewModel();

            // Customer data scoping: customers should only see their own tickets
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isCustomer = User.IsInRole(Constants.RoleCustomer);

            if (isCustomer && !string.IsNullOrEmpty(userId))
            {
                // Force filter to only show this customer's tickets
                searchModel.CustomerId = userId;
            }

            var result = await _ticketService.SearchTicketsAsync(searchModel);

            // Populate dropdowns for filter UI
            // Only show customer dropdown for non-customer users
            if (!isCustomer)
            {
                result.Customers = await _ticketService.GetCustomerSelectListAsync();
            }
            result.Employees = await _ticketService.GetEmployeeSelectListAsync();
            result.Projects = await _ticketService.GetProjectSelectListAsync();

            // Load saved filters for the current user
            if (!string.IsNullOrEmpty(userId))
            {
                ViewBag.SavedFilters = await _context.SavedFilters
                    .Where(f => f.UserId == userId)
                    .OrderBy(f => f.Name)
                    .ToListAsync();
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

        var filter = new SavedFilter
        {
            Id = Guid.NewGuid(),
            Name = name,
            UserId = userId,
            SearchTerm = searchModel.SearchTerm,
            Status = searchModel.Status,
            TicketType = searchModel.TicketType,
            ProjectId = searchModel.ProjectId,
            AssignedToId = searchModel.AssignedToId,
            CustomerId = searchModel.CustomerId,
            IsOverdue = searchModel.IsOverdue,
            IsDueSoon = searchModel.IsDueSoon
        };

        _context.SavedFilters.Add(filter);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Filter saved successfully.";
        return RedirectToAction(nameof(Index), searchModel);
    }

    [HttpGet]
    public async Task<IActionResult> LoadFilter(Guid id)
    {
        var filter = await _context.SavedFilters.FindAsync(id);
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
        var filter = await _context.SavedFilters.FindAsync(id);
        if (filter == null) return NotFound();

        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (filter.UserId != userId) return Forbid();

        _context.SavedFilters.Remove(filter);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Filter deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = User.IsInRole(Constants.RoleCustomer);

        // Only show customer dropdown for non-customer users
        if (!isCustomer)
        {
            ViewBag.Customers = await _ticketService.GetCustomerSelectListAsync();
        }
        else
        {
            // Pre-populate customer ID for customer users
            ViewBag.PreselectedCustomerId = userId;
        }

        ViewBag.Employees = await _ticketService.GetEmployeeSelectListAsync();
        ViewBag.Projects = await _ticketService.GetProjectSelectListAsync();
        ViewBag.IsCustomer = isCustomer;

        // Load domain configuration for dynamic work item types and custom fields
        var defaultDomain = _domainConfig.GetDefaultDomainId();
        ViewBag.DomainId = defaultDomain;
        ViewBag.EntityLabels = _domainConfig.GetEntityLabels(defaultDomain);
        ViewBag.WorkItemTypes = _domainConfig.GetWorkItemTypes(defaultDomain).ToList();
        ViewBag.CustomFields = _domainConfig.GetCustomFields(defaultDomain).ToList();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTicketViewModel model)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = User.IsInRole(Constants.RoleCustomer);

        // Customer data scoping: override customer ID for customer users
        if (isCustomer && !string.IsNullOrEmpty(userId))
        {
            model.CustomerId = userId;
        }

        // Validate that customer is assigned
        if (string.IsNullOrEmpty(model.CustomerId))
        {
            ModelState.AddModelError("CustomerId", "Customer must be specified");
        }

        if (!ModelState.IsValid)
        {
            // Reload dropdowns and domain config
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

            return View(model);
        }

        try
        {
            // Create ticket via service
            var ticket = await _ticketService.CreateTicketAsync(model.Description, model.CustomerId, model.ResponsibleId, model.ProjectGuid, model.CompletionTarget);

            // Set domain extensibility fields
            ticket.DomainId = model.DomainId ?? _domainConfig.GetDefaultDomainId();
            ticket.WorkItemTypeCode = model.WorkItemTypeCode;

            // Extract custom fields from form and serialize to JSON
            var formDictionary = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
            ticket.CustomFieldsJson = _ticketService.ParseCustomFields(ticket.DomainId, formDictionary);

            await _ticketService.UpdateTicketAsync(ticket);

            // Process with GERDA AI (if available)
            if (_gerdaService != null)
            {
                _logger.LogInformation("Processing ticket {TicketGuid} with GERDA AI (Domain: {DomainId}, Type: {WorkItemTypeCode})",
                    ticket.Guid, ticket.DomainId, ticket.WorkItemTypeCode);
                await _gerdaService.ProcessTicketAsync(ticket.Guid);

                var entityLabel = _domainConfig.GetEntityLabels(ticket.DomainId).WorkItem;
                TempData["Success"] = $"{entityLabel} created successfully! GERDA AI has processed the {entityLabel.ToLower()} (estimated effort, priority, and tags assigned).";
                _logger.LogInformation("GERDA processing completed for ticket {TicketGuid}", ticket.Guid);
            }
            else
            {
                var entityLabel = _domainConfig.GetEntityLabels(ticket.DomainId).WorkItem;
                TempData["Success"] = $"{entityLabel} created successfully!";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating or processing ticket");
            TempData["Warning"] = "Creation encountered an error. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Detail(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var viewModel = await _ticketService.GetTicketDetailsAsync(id.Value);

        if (viewModel == null)
        {
            return NotFound();
        }

        // Load attachments
        viewModel.Attachments = await _context.Documents
            .Where(d => d.TicketId == id.Value)
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync();

        // Load audit logs
        viewModel.AuditLogs = await _auditService.GetAuditLogForTicketAsync(id.Value);

        // Load comments
        viewModel.Comments = await _context.TicketComments
            .Include(c => c.Author)
            .Where(c => c.TicketId == id.Value)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        // Load reviews
        // viewModel.ReviewStatus is already populated by TicketService if added there
        // viewModel.ReviewStatus = ticket.ReviewStatus;
        viewModel.QualityReviews = await _context.QualityReviews
            .Include(r => r.Reviewer)
            .Where(r => r.TicketId == id.Value)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        // Get recommended agent from Dispatching service (if ticket is unassigned)
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
                            // Calculate current workload using service
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

        // Pass domain configuration for custom fields display
        var domainId = viewModel.DomainId ?? _domainConfig.GetDefaultDomainId();
        ViewBag.DomainId = domainId;
        ViewBag.EntityLabels = _domainConfig.GetEntityLabels(domainId);
        ViewBag.CustomFields = _domainConfig.GetCustomFields(domainId).ToList();
        ViewBag.WorkItemTypeCode = viewModel.WorkItemTypeCode;

        // Parse existing custom field values
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



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignToRecommended(Guid ticketGuid, string agentId)
    {
        var success = await _ticketService.AssignTicketAsync(ticketGuid, agentId);

        if (!success)
        {
            TempData["Error"] = "Failed to assign ticket. Please try again.";
            return RedirectToAction(nameof(Index));
        }

        var agent = await _ticketService.GetEmployeeByIdAsync(agentId);
        TempData["Success"] = $"Ticket successfully assigned to {agent?.FirstName} {agent?.LastName}!";
        return RedirectToAction(nameof(Detail), new { id = ticketGuid });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();

        var ticket = await _ticketService.GetTicketForEditAsync(id.Value);

        if (ticket == null) return NotFound();

        // Get all users for the dropdown
        var responsibleUsers = await _ticketService.GetAllUsersSelectListAsync();

        // Map the database data to the ViewModel
        var viewModel = new EditTicketViewModel
        {
            Guid = ticket.Guid,
            Description = ticket.Description,
            TicketStatus = ticket.TicketStatus,
            CompletionTarget = ticket.CompletionTarget,
            ResponsibleUserId = ticket.Responsible?.Id, // ID of current responsible

            // Fill the dropdown list
            ResponsibleUsers = responsibleUsers
        };

        // Pass domain configuration for custom fields
        var domainId = ticket.DomainId ?? _domainConfig.GetDefaultDomainId();
        ViewBag.DomainId = domainId;
        ViewBag.EntityLabels = _domainConfig.GetEntityLabels(domainId);
        ViewBag.CustomFields = _domainConfig.GetCustomFields(domainId).ToList();
        ViewBag.WorkItemTypeCode = ticket.WorkItemTypeCode;

        // Parse existing custom field values
        if (!string.IsNullOrEmpty(ticket.CustomFieldsJson))
        {
            try
            {
                ViewBag.CustomFieldValues = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(ticket.CustomFieldsJson);
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

        // Filter Valid Next States
        var validStates = _ruleEngine.GetValidNextStates(ticket, User);
        // Ensure allowed transitions + current status are included
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

            // Update properties based on the ViewModel
            ticketToUpdate.Description = viewModel.Description;
            ticketToUpdate.TicketStatus = viewModel.TicketStatus;
            ticketToUpdate.CompletionTarget = viewModel.CompletionTarget;

            // Extract custom fields from form and serialize to JSON
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

        // If validation fails, reload dropdowns
        viewModel.ResponsibleUsers = await _ticketService.GetAllUsersSelectListAsync();

        // Reload valid statuses
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



    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Constants.RoleEmployee + "," + Constants.RoleAdmin)]
    public async Task<IActionResult> BatchAssign(List<Guid> ticketIds, string agentId)
    {
        if (ticketIds != null && ticketIds.Any() && !string.IsNullOrEmpty(agentId))
        {
            await _ticketService.BatchAssignToAgentAsync(ticketIds, agentId);
            TempData["Success"] = $"{ticketIds.Count} ticket(s) assigned successfully.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Constants.RoleEmployee + "," + Constants.RoleAdmin)]
    public async Task<IActionResult> BatchStatus(List<Guid> ticketIds, Status status)
    {
        if (ticketIds != null && ticketIds.Any())
        {
            await _ticketService.BatchUpdateStatusAsync(ticketIds, status);
            TempData["Success"] = $"{ticketIds.Count} ticket(s) updated successfully.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportTickets(TicketSearchViewModel searchModel)
    {
        try
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isCustomer = User.IsInRole(Constants.RoleCustomer);

            // Apply customer filter for customer users
            if (isCustomer && !string.IsNullOrEmpty(userId))
            {
                searchModel.CustomerId = userId;
            }

            var result = await _ticketService.SearchTicketsAsync(searchModel);

            // Generate CSV
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Guid,Description,Status,Customer,Responsible,CreationDate,CompletionTarget");

            foreach (var ticket in result.Results)
            {
                csv.AppendLine($"{ticket.Guid},\"{ticket.Description}\",{ticket.TicketStatus},\"{ticket.Customer?.Name}\",\"{ticket.Responsible?.Name}\",{ticket.CreationDate:yyyy-MM-dd},{ticket.CompletionTarget:yyyy-MM-dd}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"tickets-export-{DateTime.Now:yyyyMMdd}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting tickets");
            TempData["Error"] = "Failed to export tickets.";
            return RedirectToAction(nameof(Index));
        }
    }
}
