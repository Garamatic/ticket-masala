using TicketMasala.Web;
using TicketMasala.Web.Models;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Web.ViewModels.Projects;
using TicketMasala.Web.ViewModels.Customers;
using TicketMasala.Web.Services.Core;
using TicketMasala.Web.Services.Tickets;
using TicketMasala.Web.Services.Projects;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Services.Background;
using TicketMasala.Web.Engine.GERDA;
using TicketMasala.Web.Services.Configuration;
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
        private readonly IFileService _fileService;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;
        private readonly IDomainConfigurationService _domainConfig;
        private readonly MasalaDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRuleEngineService _ruleEngine;
        private readonly ILogger<TicketController> _logger;

        public TicketController(
            IGerdaService gerdaService,
            ITicketService ticketService,
            IFileService fileService,
            IAuditService auditService,
            INotificationService notificationService,
            IDomainConfigurationService domainConfig,
            MasalaDbContext context,
            IHttpContextAccessor httpContextAccessor,
            IRuleEngineService ruleEngine,
            ILogger<TicketController> logger)
        {
            _gerdaService = gerdaService;
            _ticketService = ticketService;
            _fileService = fileService;
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
                
                var result = await _ticketService.SearchTicketsAsync(searchModel);
                
                // Populate dropdowns for filter UI
                ViewBag.Customers = await _ticketService.GetCustomerSelectListAsync();
                ViewBag.Employees = await _ticketService.GetEmployeeSelectListAsync();
                ViewBag.Projects = await _ticketService.GetProjectSelectListAsync();
                
                // Load saved filters for the current user
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    ViewBag.SavedFilters = await _context.SavedFilters
                        .Where(f => f.UserId == userId)
                        .OrderBy(f => f.Name)
                        .ToListAsync();
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
                var customFieldValues = new Dictionary<string, object?>();
                var customFieldDefs = _domainConfig.GetCustomFields(ticket.DomainId);
                
                foreach (var field in customFieldDefs)
                {
                    var formKey = $"customFields[{field.Name}]";
                    if (Request.Form.TryGetValue(formKey, out var values))
                    {
                        var value = values.FirstOrDefault();
                        if (!string.IsNullOrEmpty(value))
                        {
                            // Convert to appropriate type
                            customFieldValues[field.Name] = field.Type.ToLowerInvariant() switch
                            {
                                "number" or "currency" => decimal.TryParse(value, out var num) ? num : value,
                                "boolean" => value.Equals("true", StringComparison.OrdinalIgnoreCase),
                                _ => value
                            };
                        }
                    }
                }
                
                if (customFieldValues.Count > 0)
                {
                    ticket.CustomFieldsJson = System.Text.Json.JsonSerializer.Serialize(customFieldValues);
                }
                
                await _ticketService.UpdateTicketAsync(ticket);

                // Process with GERDA AI
                _logger.LogInformation("Processing ticket {TicketGuid} with GERDA AI (Domain: {DomainId}, Type: {WorkItemTypeCode}, CustomFields: {CustomFieldCount})", 
                    ticket.Guid, ticket.DomainId, ticket.WorkItemTypeCode, customFieldValues.Count);
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
                .OrderByDescending(r => r.ReviewDate)
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
        public async Task<IActionResult> AddComment(Guid id, string commentBody, bool isInternal)
        {
            if (!string.IsNullOrWhiteSpace(commentBody))
            {
                var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                try 
                {
                    await _ticketService.AddCommentAsync(id, commentBody, isInternal, currentUserId);
                }
                catch (ArgumentException)
                {
                    return NotFound();
                }
            }

            return RedirectToAction(nameof(Detail), new { id = id });
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
                var customFieldValues = new Dictionary<string, object?>();
                var customFieldDefs = _domainConfig.GetCustomFields(domainId);
                
                foreach (var field in customFieldDefs)
                {
                    var formKey = $"customFields[{field.Name}]";
                    if (Request.Form.TryGetValue(formKey, out var values))
                    {
                        var value = values.FirstOrDefault();
                        if (!string.IsNullOrEmpty(value))
                        {
                            customFieldValues[field.Name] = field.Type.ToLowerInvariant() switch
                            {
                                "number" or "currency" => decimal.TryParse(value, out var num) ? num : value,
                                "boolean" => value.Equals("true", StringComparison.OrdinalIgnoreCase),
                                _ => value
                            };
                        }
                    }
                }
                
                if (customFieldValues.Count > 0)
                {
                    ticketToUpdate.CustomFieldsJson = System.Text.Json.JsonSerializer.Serialize(customFieldValues);
                }

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
        public async Task<IActionResult> UploadAttachment(Guid ticketId, IFormFile file, bool isPublic)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Detail), new { id = ticketId });
            }

            try
            {
                var storedFileName = await _fileService.SaveFileAsync(file, "tickets");
                
                var document = new Document
                {
                    Id = Guid.NewGuid(),
                    TicketId = ticketId,
                    FileName = file.FileName,
                    StoredFileName = storedFileName,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    UploadDate = DateTime.UtcNow,
                    UploaderId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    IsPublic = isPublic
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                TempData["Success"] = "File uploaded successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file for ticket {TicketId}", ticketId);
                TempData["Error"] = "An error occurred while uploading the file.";
            }

            return RedirectToAction(nameof(Detail), new { id = ticketId });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(Guid id)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            var stream = await _fileService.GetFileStreamAsync(doc.StoredFileName, "tickets");
            if (stream == null) return NotFound();

            return File(stream, doc.ContentType, doc.FileName);
        }

        [HttpGet]
        public async Task<IActionResult> PreviewAttachment(Guid id)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            var stream = await _fileService.GetFileStreamAsync(doc.StoredFileName, "tickets");
            if (stream == null) return NotFound();

            // Return inline for preview
            Response.Headers.Append("Content-Disposition", $"inline; filename=\"{doc.FileName}\"");
            return File(stream, doc.ContentType);
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
