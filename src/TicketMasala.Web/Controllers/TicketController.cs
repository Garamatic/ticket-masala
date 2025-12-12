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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Data;

namespace TicketMasala.Web.Controllers;

[Authorize] // All authenticated users can access tickets
public class TicketController : Controller
{
    private readonly IGerdaService _gerdaService;
    private readonly ITicketService _ticketService;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly IDomainConfigurationService _domainConfig;
    private readonly ISavedFilterService _savedFilterService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRuleEngineService _ruleEngine;
    private readonly ILogger<TicketController> _logger;

    public TicketController(
        IGerdaService gerdaService,
        ITicketService ticketService,
        IAuditService auditService,
        INotificationService notificationService,
        IDomainConfigurationService domainConfig,
        ISavedFilterService savedFilterService,
        IHttpContextAccessor httpContextAccessor,
        IRuleEngineService ruleEngine,
        ILogger<TicketController> logger)
    {
        _gerdaService = gerdaService;
        _ticketService = ticketService;
        _auditService = auditService;
        _notificationService = notificationService;
        _domainConfig = domainConfig;
        _savedFilterService = savedFilterService;
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

            var result = await _ticketService.SearchTicketsAsync(searchModel);

            // Populate dropdowns for filter UI
            // Populate dropdowns for filter UI
            result.Customers = await _ticketService.GetCustomerSelectListAsync();
            result.Employees = await _ticketService.GetEmployeeSelectListAsync();
            result.Projects = await _ticketService.GetProjectSelectListAsync();

            // Load saved filters for the current user
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                ViewBag.SavedFilters = await _savedFilterService.GetFiltersForUserAsync(userId);
            }

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

        // Delegate to service
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

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Customers = await _ticketService.GetCustomerSelectListAsync();
        ViewBag.Employees = await _ticketService.GetEmployeeSelectListAsync();
        ViewBag.Projects = await _ticketService.GetProjectSelectListAsync();

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
    public async Task<IActionResult> Create(
        string description,
        string customerId,
        string? responsibleId,
        Guid? projectGuid,
        DateTime? completionTarget,
        string? domainId,
        string? workItemTypeCode)
    {
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
            // Reload dropdowns and domain config
            ViewBag.Customers = await _ticketService.GetCustomerSelectListAsync();
            ViewBag.Employees = await _ticketService.GetEmployeeSelectListAsync();
            ViewBag.Projects = await _ticketService.GetProjectSelectListAsync();

            var reloadDomain = _domainConfig.GetDefaultDomainId();
            ViewBag.DomainId = reloadDomain;
            ViewBag.EntityLabels = _domainConfig.GetEntityLabels(reloadDomain);
            ViewBag.WorkItemTypes = _domainConfig.GetWorkItemTypes(reloadDomain).ToList();
            ViewBag.CustomFields = _domainConfig.GetCustomFields(reloadDomain).ToList();

            return View();
        }

        try
        {
            // Create ticket via service
            var ticket = await _ticketService.CreateTicketAsync(description, customerId, responsibleId, projectGuid, completionTarget);

            // Set domain extensibility fields
            ticket.DomainId = domainId ?? _domainConfig.GetDefaultDomainId();
            ticket.WorkItemTypeCode = workItemTypeCode;

            // Extract custom fields from form and serialize to JSON
            var formDictionary = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
            ticket.CustomFieldsJson = _ticketService.ParseCustomFields(ticket.DomainId, formDictionary);

            await _ticketService.UpdateTicketAsync(ticket);

            // Process with GERDA AI
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

        // All related data (Attachments, AuditLogs, Comments, QualityReviews, RecommendedAgent) 
        // is now populated by TicketService.GetTicketDetailsAsync
        // except RecommendedAgent which is populated if empty? 
        // Wait, TicketService.GetTicketDetailsAsync does NOT populate RecommendedAgent in my new code?
        // Let's check. I did not add RecommendedAgent logic to TicketService.
        // So I must keep RecommendedAgent logic here OR move it to Service.
        // The Plan said "Centralize Ticket Details Fetching".
        // I should have moved RecommendedAgent too?
        // TicketController logic uses IDispatchingService (via Service Locator?!).
        // I should move this logic to TicketService.GetTicketDetailsAsync properly in a subsequent step if missed.
        // But for now, I will keep the RecommendedAgent logic here if I can't move it easily.
        // Wait, TicketService has NO IDispatchingService dependency.
        // I should move it.
        // But for this step "Remove Direct DbContext Dependency" is key.
        // RecommendedAgent logic uses dispatchingService, NOT DbContext directly (it uses _ticketService for employee lookup).
        // So I can keep it for now and just remove the DB context parts.

        // DB Context parts to remove:
        // - Attachments fetching
        // - AuditLogs fetching (was via service, but moved to TicketService logic)
        // - Comments fetching
        // - QualityReviews fetching

        // So I replace the big block of fetching with NOTHING (since viewModel already has them).

        // However, I need to check if I need to run the code for RecommendedAgent.
        // The original code ran it "if (string.IsNullOrWhiteSpace(viewModel.ResponsibleId))".
        // So I keep that block.

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
    public async Task<IActionResult> AddComment(Guid id, string commentBody, bool isInternal)
    {
        if (string.IsNullOrWhiteSpace(commentBody))
        {
            if (Request.Headers.ContainsKey("HX-Request"))
            {
                return BadRequest("Comment body is required");
            }

            TempData["Error"] = "Comment cannot be empty";
            return RedirectToAction(nameof(Detail), new { id });
        }

        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        try
        {
            await _ticketService.AddCommentAsync(id, commentBody, isInternal, userId);

            if (Request.Headers.ContainsKey("HX-Request"))
            {
                // HTMX: Return the updated comment list
                var ticketDetails = await _ticketService.GetTicketDetailsAsync(id);
                return PartialView("_CommentListPartial", ticketDetails.Comments);
            }

            TempData["Success"] = "Comment added successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to ticket {TicketId}", id);
            if (Request.Headers.ContainsKey("HX-Request"))
            {
                return StatusCode(500, "Error adding comment");
            }
            TempData["Error"] = "Failed to add comment";
        }

        return RedirectToAction(nameof(Detail), new { id });
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestReview(Guid id)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _ticketService.RequestReviewAsync(id, userId);

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            var ticketDetails = await _ticketService.GetTicketDetailsAsync(id);
            return PartialView("_QualityReviewPartial", ticketDetails);
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReview(Guid id, int score, string feedback, bool approve)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _ticketService.SubmitReviewAsync(id, score, feedback, approve, userId);

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            var ticketDetails = await _ticketService.GetTicketDetailsAsync(id);
            return PartialView("_QualityReviewPartial", ticketDetails);
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchAssign(List<Guid> ticketIds, string agentId)
    {
        if (ticketIds != null && ticketIds.Any() && !string.IsNullOrEmpty(agentId))
        {
            await _ticketService.BatchAssignToAgentAsync(ticketIds, agentId);
            // We could use TempData for success message if we had a way to display it
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchStatus(List<Guid> ticketIds, Status status)
    {
        if (ticketIds != null && ticketIds.Any())
        {
            await _ticketService.BatchUpdateStatusAsync(ticketIds, status);
        }
        return RedirectToAction(nameof(Index));
    }
}
