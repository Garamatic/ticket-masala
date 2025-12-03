using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using IT_Project2526.ViewModels;
using IT_Project2526.Models;
using IT_Project2526.Utilities;
using IT_Project2526.Services;
using System.Text.Json;

namespace IT_Project2526.Controllers
{
    [Authorize(Roles = Constants.RoleAdmin)]
    public class ManagerController : Controller
    {
        private readonly ITProjectDB _context;
        private readonly ILogger<ManagerController> _logger;
        private readonly IMetricsService _metricsService;

        public ManagerController(
            ITProjectDB context, 
            ILogger<ManagerController> logger,
            IMetricsService metricsService)
        {
            _context = context;
            _logger = logger;
            _metricsService = metricsService;
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