using TicketMasala.Web.Engine.Compiler;
using TicketMasala.Web.Models;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Web.ViewModels.Projects;
using TicketMasala.Web.ViewModels.GERDA;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Observers;
using TicketMasala.Web.Engine.Core;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Data;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// Service responsible for ticket business logic.
/// Follows Information Expert and Single Responsibility principles.
/// Updated to use Repository pattern and Observer pattern.
/// </summary>
public interface ITicketService
{
    Task<List<SelectListItem>> GetCustomerSelectListAsync();
    Task<List<SelectListItem>> GetEmployeeSelectListAsync();
    Task<List<SelectListItem>> GetProjectSelectListAsync();
    Task<Guid?> GetCurrentUserDepartmentIdAsync();
    Task<Ticket> CreateTicketAsync(string description, string customerId, string? responsibleId, Guid? projectGuid, DateTime? completionTarget);
    Task<TicketDetailsViewModel?> GetTicketDetailsAsync(Guid ticketGuid);
    Task<bool> AssignTicketAsync(Guid ticketGuid, string agentId);
    Task<bool> AssignTicketWithProjectAsync(Guid ticketGuid, string? agentId, Guid? projectGuid);
    Task<IEnumerable<TicketViewModel>> GetAllTicketsAsync();
    Task<Employee?> GetEmployeeByIdAsync(string agentId);
    Task<int> GetEmployeeCurrentWorkloadAsync(string agentId);
    Task<Ticket?> GetTicketForEditAsync(Guid ticketGuid);
    Task<List<SelectListItem>> GetAllUsersSelectListAsync();
    Task<bool> UpdateTicketAsync(Ticket ticket);
    Task<BatchAssignResult> BatchAssignTicketsAsync(BatchAssignRequest request, Func<Guid, Task<string?>> getRecommendedAgent);
    Task<TicketSearchViewModel> SearchTicketsAsync(TicketSearchViewModel searchModel);
    Task BatchAssignToAgentAsync(List<Guid> ticketIds, string agentId);
    Task BatchUpdateStatusAsync(List<Guid> ticketIds, Status status);
    Task<TicketComment> AddCommentAsync(Guid ticketId, string body, bool isInternal, string authorId);
    Task<bool> RequestReviewAsync(Guid ticketId, string requesterId);
    Task<bool> SubmitReviewAsync(Guid ticketId, int score, string feedback, bool approved, string reviewerId);
    Task<TimeLog> LogTimeAsync(Guid ticketId, string userId, double hours, DateTime date, string description);
    string ParseCustomFields(string domainId, Dictionary<string, string> formValues);
    Task<DashboardStats> GetDashboardStatsAsync(string? userId, bool isCustomer);
    Task<List<TicketViewModel>> GetRecentActivityAsync(string? userId, int count);
}

/// <summary>
/// Dashboard statistics DTO
/// </summary>
public class DashboardStats
{
    public int ProjectCount { get; set; }
    public int ActiveTicketCount { get; set; }
    public int PendingTaskCount { get; set; }
    public int CompletionRate { get; set; }
    public int NewProjectsThisWeek { get; set; }
    public int CompletedToday { get; set; }
    public int DueSoon { get; set; }
}

public class TicketService : ITicketService, ITicketQueryService, ITicketCommandService
{
    private readonly MasalaDbContext _context;
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IEnumerable<ITicketObserver> _observers;
    private readonly IEnumerable<ICommentObserver> _commentObservers;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRuleEngineService _ruleEngine;
    private readonly Engine.GERDA.Configuration.IDomainConfigurationService _domainConfig;
    private readonly ILogger<TicketService> _logger;
    private readonly Domain.TicketDispatchService _ticketDispatchService;
    private readonly Domain.TicketReportingService _ticketReportingService;
    private readonly Domain.TicketNotificationService _ticketNotificationService;

    public TicketService(
        MasalaDbContext context,
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        IEnumerable<ITicketObserver> observers,
        IEnumerable<ICommentObserver> commentObservers,
        INotificationService notificationService,
        IAuditService auditService,
        IHttpContextAccessor httpContextAccessor,
        IRuleEngineService ruleEngine,
        Engine.GERDA.Configuration.IDomainConfigurationService domainConfig,
        ILogger<TicketService> logger,
        Domain.TicketDispatchService ticketDispatchService,
        Domain.TicketReportingService ticketReportingService,
        Domain.TicketNotificationService ticketNotificationService)
    {
        _context = context;
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _observers = observers;
        _commentObservers = commentObservers;
        _notificationService = notificationService;
        _auditService = auditService;
        _httpContextAccessor = httpContextAccessor;
        _ruleEngine = ruleEngine;
        _domainConfig = domainConfig;
        _logger = logger;
        _ticketDispatchService = ticketDispatchService;
        _ticketReportingService = ticketReportingService;
        _ticketNotificationService = ticketNotificationService;
    }

    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? null;
    }

    public async Task<Guid?> GetCurrentUserDepartmentIdAsync()
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return null;

        var user = await _context.Users.FindAsync(userId);
        var employee = user as Employee;
        // Parse DepartmentId string to Guid
        if (employee?.DepartmentId != null && Guid.TryParse(employee.DepartmentId, out var deptGuid))
        {
            return deptGuid;
        }
        return null;
    }

    /// <summary>
    /// Get customer dropdown list
    /// </summary>
    public async Task<List<SelectListItem>> GetCustomerSelectListAsync()
    {
        var customers = await _userRepository.GetAllCustomersAsync();
        return customers.Select(c => new SelectListItem
        {
            Value = c.Id,
            Text = $"{c.FirstName} {c.LastName}"
        }).ToList();
    }

    /// <summary>
    /// Get employee dropdown list
    /// </summary>
    public async Task<List<SelectListItem>> GetEmployeeSelectListAsync()
    {
        var employees = await _userRepository.GetAllEmployeesAsync();
        return employees.Select(e => new SelectListItem
        {
            Value = e.Id,
            Text = $"{e.FirstName} {e.LastName}"
        }).ToList();
    }

    /// <summary>
    /// Get project dropdown list
    /// </summary>
    public async Task<List<SelectListItem>> GetProjectSelectListAsync()
    {
        var projects = await _projectRepository.GetAllAsync();
        return projects.Select(p => new SelectListItem
        {
            Value = p.Guid.ToString(),
            Text = p.Name
        }).ToList();
    }

    /// <summary>
    /// Get all tickets with customer and responsible information
    /// </summary>
    public async Task<IEnumerable<TicketViewModel>> GetAllTicketsAsync()
    {
        var departmentId = await GetCurrentUserDepartmentIdAsync();
        var tickets = await _ticketRepository.GetAllAsync(departmentId);

        return tickets.Select(t => new TicketViewModel
        {
            Guid = t.Guid,
            Description = t.Description,
            TicketStatus = t.TicketStatus,
            CreationDate = t.CreationDate,
            CompletionTarget = t.CompletionTarget,
            ResponsibleName = t.Responsible != null
                ? $"{t.Responsible.FirstName} {t.Responsible.LastName}"
                : "Not Assigned",
            CustomerName = t.Customer != null
                ? $"{t.Customer.FirstName} {t.Customer.LastName}"
                : "Unknown"
        }).ToList();
    }

    /// <summary>
    /// Create a new ticket with proper defaults and associations
    /// Notifies observers after creation (triggers GERDA processing)
    /// </summary>
    public async Task<Ticket> CreateTicketAsync(
        string description,
        string customerId,
        string? responsibleId,
        Guid? projectGuid,
        DateTime? completionTarget)
    {
        var customer = await _userRepository.GetCustomerByIdAsync(customerId);
        if (customer == null)
        {
            throw new ArgumentException("Customer not found", nameof(customerId));
        }

        Employee? responsible = null;
        if (!string.IsNullOrWhiteSpace(responsibleId))
        {
            responsible = await _userRepository.GetEmployeeByIdAsync(responsibleId);
        }

        var ticket = new Ticket
        {
            Description = description,
            Customer = customer,
            CustomerId = customerId,
            Responsible = responsible,
            Status = responsible != null ? "Assigned" : "New",
            Title = "New Ticket", // Required property
            DomainId = "IT", // Required property
            TicketStatus = responsible != null ? TicketMasala.Web.Models.Status.Assigned : TicketMasala.Web.Models.Status.Pending,
            CompletionTarget = completionTarget ?? DateTime.UtcNow.AddDays(14),
            CreatorGuid = Guid.Parse(customer.Id),
            Comments = new List<TicketComment>()
        };

        // Add ticket via repository
        await _ticketRepository.AddAsync(ticket);

        // If a project is selected, add the ticket to that project
        if (projectGuid.HasValue && projectGuid.Value != Guid.Empty)
        {
            var project = await _projectRepository.GetByIdAsync(projectGuid.Value, includeRelations: true);

            if (project != null)
            {
                project.Tasks.Add(ticket);
                await _projectRepository.UpdateAsync(project);
            }
        }

        _logger.LogInformation("Ticket {TicketGuid} created successfully", ticket.Guid);

        // Notify observers (triggers GERDA processing automatically)
        await NotifyObserversCreatedAsync(ticket);

        // Audit Log
        await _auditService.LogActionAsync(ticket.Guid, "Created", GetCurrentUserId());

        return ticket;
    }

    /// <summary>
    /// Notify all observers that a ticket was created
    /// </summary>
    private async Task NotifyObserversCreatedAsync(Ticket ticket)
    {
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnTicketCreatedAsync(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Observer {ObserverType} failed on ticket creation", observer.GetType().Name);
                // Continue with other observers
            }
        }
    }

    /// <summary>
    /// Get detailed ticket information with GERDA insights
    /// </summary>
    public async Task<TicketDetailsViewModel?> GetTicketDetailsAsync(Guid ticketGuid)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketGuid, includeRelations: true);

        if (ticket == null)
        {
            return null;
        }

        var documents = await _ticketRepository.GetDocumentsForTicketAsync(ticketGuid);
        var comments = await _ticketRepository.GetCommentsForTicketAsync(ticketGuid);
        var reviews = await _ticketRepository.GetQualityReviewsForTicketAsync(ticketGuid);
        var logs = await _auditService.GetAuditLogForTicketAsync(ticketGuid);

        var project = await _projectRepository.GetAllAsync();
        var ticketProject = project.FirstOrDefault(p => p.Tasks.Any(t => t.Guid == ticketGuid));

        var viewModel = new TicketDetailsViewModel
        {
            Guid = ticket.Guid,
            Description = ticket.Description,
            TicketStatus = ticket.TicketStatus,
            TicketType = ticket.TicketType,
            CreationDate = ticket.CreationDate,
            CompletionTarget = ticket.CompletionTarget,
            CompletionDate = ticket.CompletionDate,
            Comments = comments.ToList(),
            Attachments = documents.ToList(),
            QualityReviews = reviews.ToList(),
            AuditLogs = logs,

            // Relationships
            ResponsibleName = ticket.Responsible != null
                ? $"{ticket.Responsible.FirstName} {ticket.Responsible.LastName}"
                : null,
            ResponsibleId = ticket.Responsible?.Id,
            CustomerName = ticket.Customer != null
                ? $"{ticket.Customer.FirstName} {ticket.Customer.LastName}"
                : null,
            CustomerId = ticket.Customer?.Id,
            ParentTicketGuid = ticket.ParentTicket?.Guid,
            ProjectGuid = ticketProject?.Guid,
            ProjectName = ticketProject?.Name,

            SubTickets = ticket.SubTickets.Select(st => new SubTicketInfo
            {
                Guid = st.Guid,
                Description = st.Description,
                TicketStatus = st.TicketStatus
            }).ToList(),

            // GERDA AI Insights
            EstimatedEffortPoints = ticket.EstimatedEffortPoints,
            PriorityScore = ticket.PriorityScore,
            GerdaTags = ticket.GerdaTags,
            AiSummary = ticket.AiSummary ?? string.Empty,

            // Review Status
            ReviewStatus = ticket.ReviewStatus,

            // Domain Extensibility Fields
            DomainId = ticket.DomainId,
            WorkItemTypeCode = ticket.WorkItemTypeCode,
            CustomFieldsJson = ticket.CustomFieldsJson
        };

        return viewModel;
    }

    /// <summary>
    /// Assign a ticket to an agent
    /// Notifies observers after assignment
    /// </summary>
    public async Task<bool> AssignTicketAsync(Guid ticketGuid, string agentId)
    {
        // Delegate to TicketDispatchService
        return await _ticketDispatchService.AssignTicketAsync(
            ticketGuid,
            agentId,
            _userRepository,
            _observers,
            _notificationService,
            _auditService,
            _httpContextAccessor
        );
    }

    /// <summary>
    /// Notify all observers that a ticket was assigned
    /// </summary>
    private async Task NotifyObserversAssignedAsync(Ticket ticket, Employee assignee)
    {
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnTicketAssignedAsync(ticket, assignee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Observer {ObserverType} failed on ticket assignment", observer.GetType().Name);
            }
        }
    }

    /// <summary>
    /// Get employee by ID for GERDA recommendation details
    /// </summary>
    public async Task<Employee?> GetEmployeeByIdAsync(string agentId)
    {
        return await _userRepository.GetEmployeeByIdAsync(agentId);
    }

    /// <summary>
    /// Calculate current workload for an employee (sum of EstimatedEffortPoints for assigned/in-progress tickets)
    /// </summary>
    public async Task<int> GetEmployeeCurrentWorkloadAsync(string agentId)
    {
        // Delegate to TicketReportingService
        return await _ticketReportingService.GetEmployeeCurrentWorkloadAsync(agentId);
    }

    /// <summary>
    /// Get ticket for editing with relations
    /// </summary>
    public async Task<Ticket?> GetTicketForEditAsync(Guid ticketGuid)
    {
        return await _ticketRepository.GetByIdAsync(ticketGuid, includeRelations: true);
    }

    /// <summary>
    /// Get all users (not just employees) for edit form dropdown
    /// </summary>
    public async Task<List<SelectListItem>> GetAllUsersSelectListAsync()
    {
        var users = await _userRepository.GetAllUsersAsync();

        return users.Select(u => new SelectListItem
        {
            Value = u.Id,
            Text = $"{u.FirstName} {u.LastName}"
        }).ToList();
    }

    /// <summary>
    /// Update an existing ticket
    /// </summary>
    public async Task<bool> UpdateTicketAsync(Ticket ticket)
    {
        try
        {
            // Validate Transition Rules
            var entry = _context.Entry(ticket);
            if (entry.State == EntityState.Modified)
            {
                var originalStatus = (Status)entry.Property(t => t.TicketStatus).OriginalValue;
                if (originalStatus != ticket.TicketStatus)
                {
                    var user = _httpContextAccessor.HttpContext?.User;
                    if (user != null)
                    {
                        // Create a temporary ticket with original status to check transition FROM that status
                        var currentStatus = ticket.TicketStatus; // Logic: new status
                        ticket.TicketStatus = originalStatus; // Set back to old status for check
                        var canTransition = _ruleEngine.CanTransition(ticket, currentStatus, user);
                        ticket.TicketStatus = currentStatus; // Restore new status
                        if (!canTransition)
                        {
                            throw new DomainRuleException($"Transition from {originalStatus} to {ticket.TicketStatus} is not allowed by domain rules.");
                        }
                    }
                }
            }

            await _ticketRepository.UpdateAsync(ticket);

            // Notify observers
            await NotifyObserversUpdatedAsync(ticket);

            // Delegate notification logic
            await _ticketNotificationService.NotifyStatusChangeAsync(ticket);

            // Audit Log
            await _auditService.LogActionAsync(ticket.Guid, "Updated", GetCurrentUserId());

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update ticket {TicketGuid}", ticket.Guid);
            return false;
        }
    }

    /// <summary>
    /// Notify all observers that a ticket was updated
    /// </summary>
    private async Task NotifyObserversUpdatedAsync(Ticket ticket)
    {
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnTicketUpdatedAsync(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Observer {ObserverType} failed on ticket update", observer.GetType().Name);
            }
        }
    }

    /// <summary>
    /// Assign a ticket to an agent and/or project (manager functionality)
    /// </summary>
    public async Task<bool> AssignTicketWithProjectAsync(Guid ticketGuid, string? agentId, Guid? projectGuid)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketGuid, includeRelations: false);

        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketGuid} not found for assignment", ticketGuid);
            return false;
        }

        Employee? agent = null;

        if (!string.IsNullOrEmpty(agentId))
        {
            agent = await _userRepository.GetEmployeeByIdAsync(agentId);

            if (agent == null)
            {
                _logger.LogWarning("Agent {AgentId} not found", agentId);
                return false;
            }

            ticket.ResponsibleId = agentId;
            ticket.TicketStatus = Status.Assigned;
        }

        if (projectGuid.HasValue)
        {
            ticket.ProjectGuid = projectGuid.Value;
        }

        await _ticketRepository.UpdateAsync(ticket);

        _logger.LogInformation("Ticket {TicketGuid} assigned to agent {AgentId} and project {ProjectGuid}",
            ticketGuid, agentId, projectGuid);

        // Notify observers if agent assigned
        if (agent != null)
        {
            await NotifyObserversAssignedAsync(ticket, agent);
        }
        else
        {
            // Just project assignment, notify update
            await NotifyObserversUpdatedAsync(ticket);
        }

        return true;
    }

    /// <summary>
    /// Batch assign tickets using GERDA recommendations or manual assignment
    /// Addresses remaining database coupling in ManagerController
    /// </summary>
    public async Task<BatchAssignResult> BatchAssignTicketsAsync(
        BatchAssignRequest request,
        Func<Guid, Task<string?>> getRecommendedAgent)
    {
        var result = new BatchAssignResult();

        // Safety: Pre-load projects for lookup if we might need them
        // This avoids N+1 query problem inside the loop
        var allProjects = await _projectRepository.GetActiveProjectsAsync();
        var projectLookup = allProjects.ToDictionary(p => p.Name, p => p.Guid, StringComparer.OrdinalIgnoreCase);

        foreach (var ticketGuid in request.TicketGuids)
        {
            try
            {
                var ticket = await _ticketRepository.GetByIdAsync(ticketGuid, includeRelations: true);

                if (ticket == null)
                {
                    result.FailureCount++;
                    result.Errors.Add($"Ticket {ticketGuid} not found");
                    continue;
                }

                string? assignedAgentId = null;
                Guid? assignedProjectGuid = null;

                // 1. Determine Assignment Strategy
                (assignedAgentId, assignedProjectGuid) = await DetermineAssignmentStrategyAsync(ticket, request, getRecommendedAgent, projectLookup);

                // 2. Apply Assignment
                Employee? assignedAgent = null;
                if (!string.IsNullOrEmpty(assignedAgentId) || assignedProjectGuid.HasValue)
                {
                    assignedAgent = await ApplyAssignmentAsync(ticket, assignedAgentId, assignedProjectGuid, request.UseGerdaRecommendations);
                }

                // 3. Build Result
                var assignedProject = assignedProjectGuid.HasValue
                    ? await _projectRepository.GetByIdAsync(assignedProjectGuid.Value, includeRelations: false)
                    : null;

                result.SuccessCount++;
                result.Assignments.Add(new TicketAssignmentDetail
                {
                    TicketGuid = ticketGuid,
                    AssignedAgentName = assignedAgent != null
                        ? $"{assignedAgent.FirstName} {assignedAgent.LastName}"
                        : null,
                    AssignedProjectName = assignedProject?.Name,
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning ticket {TicketGuid}", ticketGuid);
                result.FailureCount++;
                result.Errors.Add($"Error assigning ticket {ticketGuid}: {ex.Message}");
                result.Assignments.Add(new TicketAssignmentDetail
                {
                    TicketGuid = ticketGuid,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        return result;
    }

    private async Task<(string? AgentId, Guid? ProjectId)> DetermineAssignmentStrategyAsync(
        Ticket ticket,
        BatchAssignRequest request,
        Func<Guid, Task<string?>> getRecommendedAgent,
        Dictionary<string, Guid> projectLookup)
    {
        string? assignedAgentId = null;
        Guid? assignedProjectGuid = null;

        if (request.UseGerdaRecommendations)
        {
            // Use GERDA to recommend agent
            assignedAgentId = await getRecommendedAgent(ticket.Guid);

            // Use GERDA recommended project name if available
            if (!string.IsNullOrEmpty(ticket.RecommendedProjectName) && projectLookup.TryGetValue(ticket.RecommendedProjectName, out var projGuid))
            {
                assignedProjectGuid = projGuid;
            }
            // Fallback: Use customer-based project recommendation
            else if (ticket.ProjectGuid == null && ticket.CustomerId != null)
            {
                var recommendedProject = await _projectRepository.GetRecommendedProjectForCustomerAsync(ticket.CustomerId);
                assignedProjectGuid = recommendedProject?.Guid;
            }
        }
        else
        {
            // Use forced assignments
            assignedAgentId = request.ForceAgentId;
            assignedProjectGuid = request.ForceProjectGuid;
        }

        return (assignedAgentId, assignedProjectGuid);
    }

    private async Task<Employee?> ApplyAssignmentAsync(
        Ticket ticket,
        string? agentId,
        Guid? projectId,
        bool isRecommendation)
    {
        Employee? assignedAgent = null;

        // Apply Agent
        if (!string.IsNullOrEmpty(agentId))
        {
            assignedAgent = await _userRepository.GetEmployeeByIdAsync(agentId);
            if (assignedAgent != null)
            {
                ticket.ResponsibleId = agentId;
                ticket.TicketStatus = Status.Assigned;

                if (isRecommendation)
                {
                    ticket.GerdaTags = string.IsNullOrEmpty(ticket.GerdaTags)
                        ? "AI-Dispatched"
                        : $"{ticket.GerdaTags},AI-Dispatched";
                }
            }
        }

        // Apply Project
        if (projectId.HasValue)
        {
            ticket.ProjectGuid = projectId.Value;
        }

        await _ticketRepository.UpdateAsync(ticket);

        // Notify observers
        if (assignedAgent != null)
        {
            await NotifyObserversAssignedAsync(ticket, assignedAgent);
        }
        else if (projectId.HasValue)
        {
            await NotifyObserversUpdatedAsync(ticket);
        }

        return assignedAgent;
    }

    public async Task<TicketSearchViewModel> SearchTicketsAsync(TicketSearchViewModel searchModel)
    {
        var departmentId = await GetCurrentUserDepartmentIdAsync();

        // If user is a customer, restrict to their own tickets
        // This is a safety check, though the controller should also enforce this via the view
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            // Check if user is a customer (not an Employee)
            if (user != null && user is not Employee)
            {
                searchModel.CustomerId = userId;
            }
        }

        return await _ticketRepository.SearchTicketsAsync(searchModel, departmentId);
    }

    public async Task BatchAssignToAgentAsync(List<Guid> ticketIds, string agentId)
    {
        foreach (var id in ticketIds)
        {
            await AssignTicketAsync(id, agentId);
        }
    }

    public async Task BatchUpdateStatusAsync(List<Guid> ticketIds, Status status)
    {
        foreach (var id in ticketIds)
        {
            var ticket = await _ticketRepository.GetByIdAsync(id, includeRelations: false);
            if (ticket != null)
            {
                ticket.TicketStatus = status;
                await UpdateTicketAsync(ticket);
            }
        }
    }

    public async Task<TicketComment> AddCommentAsync(Guid ticketId, string body, bool isInternal, string authorId)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, includeRelations: true);
        if (ticket == null)
        {
            throw new ArgumentException("Ticket not found", nameof(ticketId));
        }

        var comment = new TicketComment
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Body = body,
            IsInternal = isInternal,
            CreatedAt = DateTime.UtcNow,
            AuthorId = authorId,
            Ticket = ticket // Set navigation property for observers
        };

        // We need to add the comment to the database. 
        // Since we don't have a specific CommentRepository, we can use the context directly or add to ticket collection.
        // Ideally, we should have a repository method, but for now let's use the context via a new method in TicketRepository or direct context if necessary.
        // Wait, TicketService has _context injected.
        _context.TicketComments.Add(comment);
        await _context.SaveChangesAsync();

        // Audit Log
        await _auditService.LogActionAsync(ticketId, "Commented", authorId, "Comment", null, isInternal ? "Internal Note" : "Public Reply");

        // Notify observers
        await NotifyObserversCommentedAsync(comment);

        return comment;
    }

    private async Task NotifyObserversCommentedAsync(TicketComment comment)
    {
        // Notify ticket observers (legacy)
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnTicketCommentedAsync(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Observer {ObserverType} failed on ticket comment", observer.GetType().Name);
            }
        }

        // Notify comment-specific observers (new pattern)
        foreach (var observer in _commentObservers)
        {
            try
            {
                await observer.OnCommentAddedAsync(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CommentObserver {ObserverType} failed on comment added", observer.GetType().Name);
            }
        }
    }
    public async Task<bool> RequestReviewAsync(Guid ticketId, string requesterId)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, includeRelations: false);
        if (ticket == null) return false;

        ticket.ReviewStatus = ReviewStatus.Pending;
        await _ticketRepository.UpdateAsync(ticket);

        await _auditService.LogActionAsync(ticketId, "ReviewRequested", requesterId);

        // Notify Observers? (Ideally yes, but keeping it simple for now)
        return true;
    }

    public async Task<bool> SubmitReviewAsync(Guid ticketId, int score, string feedback, bool approved, string reviewerId)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, includeRelations: false);
        if (ticket == null) return false;

        ticket.ReviewStatus = approved ? ReviewStatus.Approved : ReviewStatus.Rejected;

        var review = new QualityReview
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            ReviewerId = reviewerId,
            Score = score,
            Comments = feedback,
            CreatedAt = DateTime.UtcNow,
            IsApproved = approved
        };

        // Add review (using context directly as we lack a specific repo method in this scope, similar to comments)
        _context.QualityReviews.Add(review);
        await _ticketRepository.UpdateAsync(ticket);

        await _auditService.LogActionAsync(ticketId, approved ? "ReviewApproved" : "ReviewRejected", reviewerId, "QualityReview", null, feedback);

        return true;
    }

    public string ParseCustomFields(string domainId, Dictionary<string, string> formValues)
    {
        var customFieldValues = new Dictionary<string, object?>();
        var customFieldDefs = _domainConfig.GetCustomFields(domainId);

        foreach (var field in customFieldDefs)
        {
            var formKey = $"customFields[{field.Name}]";
            if (formValues.TryGetValue(formKey, out var value) && !string.IsNullOrEmpty(value))
            {
                customFieldValues[field.Name] = field.Type.ToLowerInvariant() switch
                {
                    "number" or "currency" => decimal.TryParse(value, out var num) ? num : value,
                    "boolean" => value.Equals("true", StringComparison.OrdinalIgnoreCase),
                    _ => value
                };
            }
        }

        return System.Text.Json.JsonSerializer.Serialize(customFieldValues);
    }

    public async Task<TimeLog> LogTimeAsync(Guid ticketId, string userId, double hours, DateTime date, string description)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, includeRelations: false);
        if (ticket == null)
        {
            throw new ArgumentException("Ticket not found", nameof(ticketId));
        }

        var timeLog = new TimeLog
        {
            TicketId = ticketId,
            UserId = userId,
            Hours = hours,
            Date = date,
            Description = description
        };

        _context.TimeLogs.Add(timeLog);
        await _context.SaveChangesAsync();

        // Audit Log
        await _auditService.LogActionAsync(ticketId, "TimeLogged", userId, "TimeLog", null, $"{hours} hours");

        _logger.LogInformation("Time logged for ticket {TicketId}: {Hours} hours by {UserId}", ticketId, hours, userId);

        return timeLog;
    }

    /// <summary>
    /// Get dashboard statistics for the home page
    /// </summary>
    public async Task<DashboardStats> GetDashboardStatsAsync(string? userId, bool isCustomer)
    {
        var stats = new DashboardStats();
        var now = DateTime.UtcNow;
        var weekAgo = now.AddDays(-7);
        var todayStart = now.Date;

        try
        {
            // Build base query with customer filtering if needed
            IQueryable<Ticket> ticketQuery = _context.Tickets.AsNoTracking();
            IQueryable<Project> projectQuery = _context.Projects.AsNoTracking();

            if (isCustomer && !string.IsNullOrEmpty(userId))
            {
                ticketQuery = ticketQuery.Where(t => t.CustomerId == userId);
                projectQuery = projectQuery.Where(p => p.CustomerId == userId);
            }

            // Project count
            stats.ProjectCount = await projectQuery.CountAsync();
            stats.NewProjectsThisWeek = await projectQuery
                .Where(p => p.CreationDate >= weekAgo)
                .CountAsync();

            // Active tickets (not completed)
            stats.ActiveTicketCount = await ticketQuery
                .Where(t => t.TicketStatus != Status.Completed)
                .CountAsync();

            // Pending tasks (status = Pending)
            stats.PendingTaskCount = await ticketQuery
                .Where(t => t.TicketStatus == Status.Pending)
                .CountAsync();

            // Completed today
            stats.CompletedToday = await ticketQuery
                .Where(t => t.TicketStatus == Status.Completed && t.CompletionDate >= todayStart)
                .CountAsync();

            // Due soon (within 3 days)
            var threeDaysFromNow = now.AddDays(3);
            stats.DueSoon = await ticketQuery
                .Where(t => t.CompletionTarget.HasValue &&
                           t.CompletionTarget.Value <= threeDaysFromNow &&
                           t.TicketStatus != Status.Completed)
                .CountAsync();

            // Completion rate (percentage of completed tickets)
            var totalTickets = await ticketQuery.CountAsync();
            var completedTickets = await ticketQuery.Where(t => t.TicketStatus == Status.Completed).CountAsync();
            stats.CompletionRate = totalTickets > 0 ? (int)((double)completedTickets / totalTickets * 100) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard stats for user {UserId}", userId);
        }

        return stats;
    }

    public async Task<List<TicketViewModel>> GetRecentActivityAsync(string? userId, int count)
    {
        IQueryable<Ticket> query = _context.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Responsible) 
            .AsNoTracking();

        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            // In this system, Customers are base ApplicationUsers, Employees are derived.
            // So if it is NOT an Employee, it is a Customer.
            if (user is not TicketMasala.Web.Models.Employee)
            {
                 query = query.Where(t => t.CustomerId == userId);
            }
        }

        var tickets = await query
            .OrderByDescending(t => t.CreationDate) // Fallback to CreationDate as LastModified is not tracked on entity
            .Take(count)
            .ToListAsync();

        return tickets.Select(t => new TicketViewModel
        {
            Guid = t.Guid,
            Description = t.Description,
            TicketStatus = t.TicketStatus,
            CreationDate = t.CreationDate,
            CompletionTarget = t.CompletionTarget,
             ResponsibleName = t.Responsible != null
                ? $"{t.Responsible.FirstName} {t.Responsible.LastName}"
                : "Unassigned",
            CustomerName = t.Customer != null
                ? $"{t.Customer.FirstName} {t.Customer.LastName}"
                : "Unknown"
        }).ToList();
    }
}
