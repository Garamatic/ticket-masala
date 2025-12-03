using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using IT_Project2526.ViewModels;
using IT_Project2526.Models;
using IT_Project2526.Utilities;
using IT_Project2526.Services;
using IT_Project2526.Services.GERDA.Dispatching;
using IT_Project2526.Services.GERDA.Ranking;
using IT_Project2526.Repositories;
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
        private readonly IDispatchBacklogService _dispatchBacklogService;
        private readonly ITicketService _ticketService;
        private readonly IProjectRepository _projectRepository;

        public ManagerController(
            ITProjectDB context, 
            ILogger<ManagerController> logger,
            IMetricsService metricsService,
            IDispatchingService dispatchingService,
            IRankingService rankingService,
            IDispatchBacklogService dispatchBacklogService,
            ITicketService ticketService,
            IProjectRepository projectRepository)
        {
            _context = context;
            _logger = logger;
            _metricsService = metricsService;
            _dispatchingService = dispatchingService;
            _rankingService = rankingService;
            _dispatchBacklogService = dispatchBacklogService;
            _ticketService = ticketService;
            _projectRepository = projectRepository;
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

        public async Task<IActionResult> Projects()
        {
            try
            {
                _logger.LogInformation("Manager viewing all projects");
                
                var allProjects = await _projectRepository.GetAllAsync();
                var projects = allProjects.Where(p => p.ValidUntil == null).ToList();

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
        /// Refactored to use DispatchBacklogService (addresses God Object anti-pattern)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DispatchBacklog()
        {
            try
            {
                _logger.LogInformation("Manager viewing GERDA Dispatch Backlog");

                var viewModel = await _dispatchBacklogService.BuildDispatchBacklogViewModelAsync();

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
                var success = await _ticketService.AssignTicketWithProjectAsync(ticketGuid, agentId, projectGuid);
                
                if (!success)
                {
                    return Json(new { success = false, message = "Ticket not found or assignment failed" });
                }

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