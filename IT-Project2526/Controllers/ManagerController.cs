using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using IT_Project2526.ViewModels;
using IT_Project2526.Models;
using IT_Project2526.Utilities;
using System.Text.Json;

namespace IT_Project2526.Controllers
{
    [Authorize(Roles = Constants.RoleAdmin)]
    public class ManagerController : Controller
    {
        private readonly ITProjectDB _context;
        private readonly ILogger<ManagerController> _logger;

        public ManagerController(ITProjectDB context, ILogger<ManagerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Team Dashboard showing GERDA AI metrics and team performance
        /// </summary>
        public async Task<IActionResult> TeamDashboard()
        {
            try
            {
                _logger.LogInformation("Manager viewing Team Dashboard with GERDA metrics");

                var viewModel = new TeamDashboardViewModel();

                // Get all active tickets with related data
                var allTickets = await _context.Tickets
                    .Include(t => t.Responsible)
                    .Include(t => t.Customer)
                    .Where(t => t.ValidUntil == null)
                    .ToListAsync();

                var activeTickets = allTickets
                    .Where(t => t.TicketStatus != Status.Completed)
                    .ToList();

                // Overall Ticket Metrics
                viewModel.TotalActiveTickets = activeTickets.Count;
                viewModel.UnassignedTickets = activeTickets.Count(t => t.ResponsibleId == null);
                viewModel.AssignedTickets = activeTickets.Count(t => t.ResponsibleId != null && t.TicketStatus != Status.Completed);
                viewModel.CompletedTickets = allTickets.Count(t => t.TicketStatus == Status.Completed);
                viewModel.OverdueTickets = activeTickets.Count(t => t.CompletionTarget.HasValue && t.CompletionTarget.Value < DateTime.UtcNow);

                // GERDA AI Metrics
                var ticketsWithPriority = activeTickets.Where(t => t.PriorityScore > 0).ToList();
                viewModel.AveragePriorityScore = ticketsWithPriority.Any() 
                    ? Math.Round(ticketsWithPriority.Average(t => t.PriorityScore), 2) 
                    : 0;

                var ticketsWithEffort = activeTickets.Where(t => t.EstimatedEffortPoints > 0).ToList();
                viewModel.AverageComplexity = ticketsWithEffort.Any() 
                    ? Math.Round(ticketsWithEffort.Average(t => t.EstimatedEffortPoints), 2) 
                    : 0;

                viewModel.AiAssignedCount = allTickets.Count(t => 
                    t.GerdaTags != null && t.GerdaTags.Contains("AI-Assigned"));

                var totalAssignments = allTickets.Count(t => t.ResponsibleId != null);
                viewModel.AiAssignmentAcceptanceRate = totalAssignments > 0 
                    ? Math.Round((double)viewModel.AiAssignedCount / totalAssignments * 100, 1) 
                    : 0;

                // SLA Metrics
                var ticketsWithSla = activeTickets.Where(t => t.CompletionTarget.HasValue).ToList();
                viewModel.TicketsWithinSla = ticketsWithSla.Count(t => t.CompletionTarget!.Value >= DateTime.UtcNow);
                viewModel.TicketsBreachingSla = ticketsWithSla.Count(t => t.CompletionTarget!.Value < DateTime.UtcNow);
                viewModel.SlaComplianceRate = ticketsWithSla.Any() 
                    ? Math.Round((double)viewModel.TicketsWithinSla / ticketsWithSla.Count * 100, 1) 
                    : 100;

                // Agent Workload Metrics
                var employees = await _context.Users
                    .OfType<Employee>()
                    .ToListAsync();

                viewModel.AgentWorkloads = employees.Select(emp =>
                {
                    var assignedTickets = activeTickets
                        .Where(t => t.ResponsibleId == emp.Id && t.TicketStatus != Status.Completed)
                        .ToList();

                    var workload = assignedTickets.Sum(t => t.EstimatedEffortPoints);
                    var maxCapacity = emp.MaxCapacityPoints > 0 ? emp.MaxCapacityPoints : 40;

                    return new AgentWorkloadMetric
                    {
                        AgentId = emp.Id,
                        AgentName = $"{emp.FirstName} {emp.LastName}",
                        AssignedTicketCount = assignedTickets.Count,
                        CurrentWorkload = workload,
                        MaxCapacity = maxCapacity,
                        UtilizationPercentage = maxCapacity > 0 
                            ? Math.Round((double)workload / maxCapacity * 100, 1) 
                            : 0
                    };
                }).OrderByDescending(a => a.UtilizationPercentage).ToList();

                // Priority Distribution
                foreach (var ticket in activeTickets.Where(t => t.PriorityScore > 0))
                {
                    var urgencyLabel = GetUrgencyLabel(ticket.PriorityScore);
                    if (viewModel.PriorityDistribution.ContainsKey(urgencyLabel))
                    {
                        viewModel.PriorityDistribution[urgencyLabel]++;
                    }
                }

                // Complexity Distribution
                foreach (var ticket in activeTickets.Where(t => t.EstimatedEffortPoints > 0))
                {
                    var complexityLabel = GetComplexityLabel(ticket.EstimatedEffortPoints);
                    if (viewModel.ComplexityDistribution.ContainsKey(complexityLabel))
                    {
                        viewModel.ComplexityDistribution[complexityLabel]++;
                    }
                }

                // Top Tags
                var allTags = activeTickets
                    .Where(t => !string.IsNullOrWhiteSpace(t.GerdaTags))
                    .SelectMany(t => t.GerdaTags!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .GroupBy(tag => tag)
                    .Select(g => new TagFrequency { TagName = g.Key, Count = g.Count() })
                    .OrderByDescending(tf => tf.Count)
                    .Take(10)
                    .ToList();

                viewModel.TopTags = allTags;

                // Recent Activity (last 20 tickets created/assigned/completed)
                var recentTickets = allTickets
                    .OrderByDescending(t => t.CreationDate)
                    .Take(20)
                    .ToList();

                viewModel.RecentActivity = recentTickets.Select(t =>
                {
                    string activityType;
                    DateTime timestamp = t.CreationDate;

                    if (t.TicketStatus == Status.Completed)
                    {
                        activityType = "Completed";
                    }
                    else if (t.ResponsibleId != null && t.TicketStatus == Status.Assigned)
                    {
                        activityType = "Assigned";
                    }
                    else
                    {
                        activityType = "Created";
                    }

                    return new RecentActivityItem
                    {
                        Timestamp = timestamp,
                        TicketGuid = t.Guid.ToString().Substring(0, 8),
                        TicketDescription = t.Description.Length > 60 
                            ? t.Description.Substring(0, 60) + "..." 
                            : t.Description,
                        ActivityType = activityType,
                        AgentName = t.Responsible != null 
                            ? $"{t.Responsible.FirstName} {t.Responsible.LastName}" 
                            : "Unassigned"
                    };
                }).ToList();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Team Dashboard");
                TempData["ErrorMessage"] = "Failed to load dashboard metrics.";
                return RedirectToAction("Projects");
            }
        }

        /// <summary>
        /// Get urgency label from priority score
        /// </summary>
        private string GetUrgencyLabel(double priorityScore)
        {
            if (priorityScore >= 15.0) return "Critical";
            if (priorityScore >= 10.0) return "High";
            if (priorityScore >= 5.0) return "Medium";
            return "Low";
        }

        /// <summary>
        /// Get complexity label from effort points (Fibonacci scale)
        /// </summary>
        private string GetComplexityLabel(int effortPoints)
        {
            if (effortPoints <= 1) return "Trivial";
            if (effortPoints <= 3) return "Simple";
            if (effortPoints <= 8) return "Medium";
            if (effortPoints <= 13) return "Complex";
            return "Very Complex";
        }

        public IActionResult Projects()
        {
            try
            {
                _logger.LogInformation("Manager viewing all projects");
                
                var projects = _context.Projects
                    .Include(p => p.Tasks)
                    .Include(p => p.ProjectManager)
                    .Include(p => p.Tasks).ThenInclude(t => t.Responsible)
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
    }
}