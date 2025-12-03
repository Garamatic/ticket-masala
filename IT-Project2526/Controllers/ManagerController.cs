using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using IT_Project2526.ViewModels;
using IT_Project2526.Models;
using IT_Project2526.Utilities;
using IT_Project2526.Services;
using IT_Project2526.Services.GERDA.Dispatching;
using IT_Project2526.Services.GERDA.Ranking;
using System.Text.Json;

namespace IT_Project2526.Controllers
{
    [Authorize(Roles = Constants.RoleAdmin)]
    public class ManagerController : Controller
    {
        private readonly ITProjectDB _context;
        private readonly ILogger<ManagerController> _logger;
        private readonly IMetricsService _metricsService;
        private readonly IDispatchingService _dispatchingService;
        private readonly IRankingService _rankingService;

        public ManagerController(
            ITProjectDB context, 
            ILogger<ManagerController> logger,
            IMetricsService metricsService,
            IDispatchingService dispatchingService,
            IRankingService rankingService)
        {
            _context = context;
            _logger = logger;
            _metricsService = metricsService;
            _dispatchingService = dispatchingService;
            _rankingService = rankingService;
        }

        /// <summary>
        /// Team Dashboard showing GERDA AI metrics and team performance
        /// </summary>
        public async Task<IActionResult> TeamDashboard()
        {
            try
            {
                _logger.LogInformation("Manager viewing Team Dashboard with GERDA metrics");

                var viewModel = await _metricsService.CalculateTeamMetricsAsync();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Team Dashboard");
                TempData["ErrorMessage"] = "Failed to load dashboard metrics.";
                return RedirectToAction("Projects");
            }
        }

        public IActionResult Projects()
        {
            try
            {
                _logger.LogInformation("Manager viewing all projects");
                
                var projects = _context.Projects
                    .Include(p => p.Tasks.Where(t => t.ValidUntil == null))
                    .Include(p => p.ProjectManager)
                    .Include(p => p.Tasks.Where(t => t.ValidUntil == null)).ThenInclude(t => t.Responsible)
                    .Where(p => p.ValidUntil == null)
                    .ToList();

                var viewModels = projects.Select(p => new ProjectTicketViewModel
                {
                    ProjectDetails = new ProjectViewModel
                    {
                        Guid = p.Guid,
                        Name = p.Name,
                        Description = p.Description,
                        Status = p.Status,
                        ProjectManagerName = p.ProjectManager != null 
                            ? $"{p.ProjectManager.FirstName} {p.ProjectManager.LastName}"
                            : "Unassigned",
                        ProjectManager = p.ProjectManager,
                        TicketCount = p.Tasks.Count
                    },
                    Tasks = p.Tasks.Select(t => new TicketViewModel
                    {
                        Guid = t.Guid,
                        Description = t.Description,
                        TicketStatus = t.TicketStatus,
                        ResponsibleName = t.Responsible != null 
                            ? $"{t.Responsible.FirstName} {t.Responsible.LastName}"
                            : "Unassigned",
                        CustomerName = string.Empty,
                        Comments = t.Comments?.ToList() ?? new List<string>(),
                        CompletionTarget = t.CompletionTarget,
                        CreationDate = DateTime.UtcNow
                    }).ToList()
                }).ToList();

                return View(viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading manager projects view");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// GERDA Dispatching Dashboard - Shows backlog with AI recommendations
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DispatchBacklog()
        {
            try
            {
                _logger.LogInformation("Manager viewing GERDA Dispatch Backlog");

                // Get unassigned or pending tickets
                var unassignedTickets = await _context.Tickets
                    .Include(t => t.Customer)
                    .Include(t => t.Project)
                    .Where(t => t.ValidUntil == null)
                    .Where(t => t.TicketStatus == Status.Pending || 
                               (t.TicketStatus == Status.Assigned && t.ResponsibleId == null))
                    .ToListAsync();
                
                // Order by CreationDate in memory (since it's a computed property)
                unassignedTickets = unassignedTickets.OrderByDescending(t => t.CreationDate).ToList();

                // Get all active employees
                var employees = await _context.Users.OfType<Employee>().ToListAsync();

                // Get current workload for each employee
                var agentWorkloads = await _context.Tickets
                    .Where(t => t.ResponsibleId != null && t.ValidUntil == null)
                    .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
                    .GroupBy(t => t.ResponsibleId)
                    .Select(g => new { AgentId = g.Key!, Count = g.Count(), EffortPoints = g.Sum(t => t.EstimatedEffortPoints) })
                    .ToDictionaryAsync(x => x.AgentId, x => (x.Count, x.EffortPoints));

                // Get active projects
                var projects = await _context.Projects
                    .Include(p => p.Customer)
                    .Where(p => p.ValidUntil == null)
                    .Where(p => p.Status == Status.Pending || p.Status == Status.InProgress)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                // Build ticket dispatch info with recommendations
                var ticketDispatchInfos = new List<TicketDispatchInfo>();

                foreach (var ticket in unassignedTickets)
                {
                    var ticketInfo = new TicketDispatchInfo
                    {
                        Guid = ticket.Guid,
                        Description = ticket.Description,
                        TicketStatus = ticket.TicketStatus,
                        CreationDate = ticket.CreationDate,
                        CompletionTarget = ticket.CompletionTarget,
                        CustomerName = ticket.Customer != null 
                            ? $"{ticket.Customer.FirstName} {ticket.Customer.LastName}" 
                            : "Unknown",
                        CustomerId = ticket.CustomerId,
                        EstimatedEffortPoints = ticket.EstimatedEffortPoints,
                        PriorityScore = ticket.PriorityScore,
                        GerdaTags = ticket.GerdaTags,
                        CurrentProjectGuid = ticket.ProjectGuid,
                        CurrentProjectName = ticket.Project?.Name
                    };

                    // Get GERDA agent recommendations
                    if (_dispatchingService.IsEnabled)
                    {
                        try
                        {
                            var recommendations = await _dispatchingService.GetTopRecommendedAgentsAsync(ticket.Guid, count: 3);
                            
                            foreach (var (agentId, score) in recommendations)
                            {
                                var agent = employees.FirstOrDefault(e => e.Id == agentId);
                                if (agent != null)
                                {
                                    var workload = agentWorkloads.GetValueOrDefault(agentId, (0, 0));
                                    
                                    ticketInfo.RecommendedAgents.Add(new AgentRecommendation
                                    {
                                        AgentId = agentId,
                                        AgentName = $"{agent.FirstName} {agent.LastName}",
                                        Team = agent.Team,
                                        Score = score,
                                        CurrentWorkload = workload.Item1,
                                        MaxCapacity = agent.MaxCapacityPoints,
                                        Specializations = agent.Specializations,
                                        Language = agent.Language,
                                        Region = agent.Region
                                    });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to get GERDA recommendations for ticket {TicketGuid}", ticket.Guid);
                        }
                    }

                    // Recommend project based on customer
                    if (ticket.ProjectGuid == null && ticket.Customer != null)
                    {
                        var customerProjects = projects.Where(p => p.CustomerId == ticket.CustomerId).ToList();
                        if (customerProjects.Any())
                        {
                            // Recommend the most recent active project for this customer
                            var recommendedProject = customerProjects
                                .OrderByDescending(p => p.CreationDate)
                                .FirstOrDefault();
                            
                            if (recommendedProject != null)
                            {
                                ticketInfo.RecommendedProjectGuid = recommendedProject.Guid;
                                ticketInfo.RecommendedProjectName = recommendedProject.Name;
                            }
                        }
                    }

                    ticketDispatchInfos.Add(ticketInfo);
                }

                // Build agent info list
                var agentInfos = employees.Select(e =>
                {
                    var workload = agentWorkloads.GetValueOrDefault(e.Id, (0, 0));
                    return new AgentInfo
                    {
                        Id = e.Id,
                        Name = $"{e.FirstName} {e.LastName}",
                        Team = e.Team,
                        CurrentWorkload = workload.Item1,
                        MaxCapacity = 10, // Default max tickets
                        CurrentEffortPoints = workload.Item2,
                        MaxCapacityPoints = e.MaxCapacityPoints,
                        Language = e.Language,
                        Region = e.Region
                    };
                }).OrderBy(a => a.Team).ThenBy(a => a.Name).ToList();

                // Calculate statistics
                var now = DateTime.UtcNow;
                var statistics = new DispatchStatistics
                {
                    TotalUnassignedTickets = ticketDispatchInfos.Count,
                    TicketsWithProjectRecommendation = ticketDispatchInfos.Count(t => t.RecommendedProjectGuid.HasValue),
                    TicketsWithAgentRecommendation = ticketDispatchInfos.Count(t => t.RecommendedAgents.Any()),
                    TotalAvailableAgents = agentInfos.Count(a => a.IsAvailable),
                    OverloadedAgents = agentInfos.Count(a => a.WorkloadPercentage >= 100),
                    AverageTicketAge = unassignedTickets.Any() 
                        ? unassignedTickets.Average(t => (now - t.CreationDate).TotalHours) 
                        : 0,
                    HighPriorityTickets = ticketDispatchInfos.Count(t => t.PriorityScore >= 50),
                    TicketsOlderThan24Hours = ticketDispatchInfos.Count(t => t.TimeInBacklog.TotalHours >= 24)
                };

                // Build project options
                var projectOptions = projects.Select(p => new ProjectOption
                {
                    Guid = p.Guid,
                    Name = p.Name,
                    CustomerName = p.Customer != null 
                        ? $"{p.Customer.FirstName} {p.Customer.LastName}" 
                        : "Unknown",
                    CurrentTicketCount = p.Tasks.Count,
                    Status = p.Status
                }).ToList();

                var viewModel = new GerdaDispatchViewModel
                {
                    UnassignedTickets = ticketDispatchInfos,
                    AvailableAgents = agentInfos,
                    Statistics = statistics,
                    Projects = projectOptions
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading GERDA Dispatch Backlog");
                TempData["ErrorMessage"] = "Failed to load dispatch backlog.";
                return RedirectToAction("TeamDashboard");
            }
        }

        /// <summary>
        /// Assign a single ticket to an agent and/or project
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTicket(Guid ticketGuid, string? agentId, Guid? projectGuid)
        {
            try
            {
                var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Guid == ticketGuid);
                if (ticket == null)
                {
                    return Json(new { success = false, message = "Ticket not found" });
                }

                if (!string.IsNullOrEmpty(agentId))
                {
                    ticket.ResponsibleId = agentId;
                    ticket.TicketStatus = Status.Assigned;
                }

                if (projectGuid.HasValue)
                {
                    ticket.ProjectGuid = projectGuid.Value;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Manager assigned ticket {TicketGuid} to agent {AgentId} and project {ProjectGuid}",
                    ticketGuid, agentId, projectGuid);

                return Json(new { success = true, message = "Ticket assigned successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning ticket {TicketGuid}", ticketGuid);
                return Json(new { success = false, message = "Error assigning ticket" });
            }
        }

        /// <summary>
        /// Batch assign tickets using GERDA recommendations or manual assignment
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BatchAssignTickets([FromBody] BatchAssignRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Manager batch assigning {Count} tickets, UseGerda={UseGerda}",
                    request.TicketGuids.Count, request.UseGerdaRecommendations);

                var result = new BatchAssignResult();

                foreach (var ticketGuid in request.TicketGuids)
                {
                    try
                    {
                        var ticket = await _context.Tickets
                            .Include(t => t.Responsible)
                            .Include(t => t.Project)
                            .FirstOrDefaultAsync(t => t.Guid == ticketGuid);

                        if (ticket == null)
                        {
                            result.FailureCount++;
                            result.Errors.Add($"Ticket {ticketGuid} not found");
                            continue;
                        }

                        string? assignedAgentId = null;
                        Guid? assignedProjectGuid = null;

                        // Determine assignment strategy
                        if (request.UseGerdaRecommendations)
                        {
                            // Use GERDA to recommend agent
                            if (_dispatchingService.IsEnabled)
                            {
                                assignedAgentId = await _dispatchingService.GetRecommendedAgentAsync(ticketGuid);
                            }

                            // Use customer-based project recommendation
                            if (ticket.ProjectGuid == null && ticket.CustomerId != null)
                            {
                                var customerProject = await _context.Projects
                                    .Where(p => p.CustomerId == ticket.CustomerId && p.ValidUntil == null)
                                    .Where(p => p.Status == Status.Pending || p.Status == Status.InProgress)
                                    .OrderByDescending(p => p.CreationDate)
                                    .FirstOrDefaultAsync();
                                
                                assignedProjectGuid = customerProject?.Guid;
                            }
                        }
                        else
                        {
                            // Use forced assignments
                            assignedAgentId = request.ForceAgentId;
                            assignedProjectGuid = request.ForceProjectGuid;
                        }

                        // Apply assignments
                        if (!string.IsNullOrEmpty(assignedAgentId))
                        {
                            ticket.ResponsibleId = assignedAgentId;
                            ticket.TicketStatus = Status.Assigned;
                            
                            if (request.UseGerdaRecommendations)
                            {
                                ticket.GerdaTags = string.IsNullOrEmpty(ticket.GerdaTags)
                                    ? "AI-Dispatched"
                                    : $"{ticket.GerdaTags},AI-Dispatched";
                            }
                        }

                        if (assignedProjectGuid.HasValue)
                        {
                            ticket.ProjectGuid = assignedProjectGuid.Value;
                        }

                        await _context.SaveChangesAsync();

                        // Get assigned names for result
                        var assignedAgent = assignedAgentId != null
                            ? await _context.Users.OfType<Employee>().FirstOrDefaultAsync(e => e.Id == assignedAgentId)
                            : null;
                        
                        var assignedProject = assignedProjectGuid.HasValue
                            ? await _context.Projects.FirstOrDefaultAsync(p => p.Guid == assignedProjectGuid.Value)
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

                _logger.LogInformation(
                    "Batch assignment complete: {Success} succeeded, {Failed} failed",
                    result.SuccessCount, result.FailureCount);

                return Json(new 
                { 
                    success = true, 
                    successCount = result.SuccessCount,
                    failureCount = result.FailureCount,
                    assignments = result.Assignments,
                    errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch assignment");
                return Json(new { success = false, message = "Batch assignment failed" });
            }
        }

        /// <summary>
        /// Auto-dispatch a single ticket using GERDA
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoDispatchTicket(Guid ticketGuid)
        {
            try
            {
                if (!_dispatchingService.IsEnabled)
                {
                    return Json(new { success = false, message = "GERDA Dispatching is disabled" });
                }

                var success = await _dispatchingService.AutoDispatchTicketAsync(ticketGuid);

                if (success)
                {
                    var ticket = await _context.Tickets
                        .Include(t => t.Responsible)
                        .FirstOrDefaultAsync(t => t.Guid == ticketGuid);
                    
                    var agentName = ticket?.Responsible != null
                        ? $"{ticket.Responsible.FirstName} {ticket.Responsible.LastName}"
                        : "Unknown";

                    return Json(new 
                    { 
                        success = true, 
                        message = $"Ticket dispatched to {agentName}",
                        agentName = agentName
                    });
                }

                return Json(new { success = false, message = "No suitable agent found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-dispatching ticket {TicketGuid}", ticketGuid);
                return Json(new { success = false, message = "Error auto-dispatching ticket" });
            }
        }
    }
}