using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.ViewModels.Dashboard;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Web.ViewModels.GERDA;
using TicketMasala.Web.ViewModels.Projects;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Engine.Ingestion.Background;
using TicketMasala.Web.Engine.GERDA.Dispatching;
using TicketMasala.Web.Engine.GERDA.Ranking;
using TicketMasala.Web.Engine.GERDA.Anticipation;
using TicketMasala.Web.Repositories;
using System.Text.Json;
using Newtonsoft.Json;

namespace TicketMasala.Web.Controllers;

[Authorize(Roles = Constants.RoleAdmin)]
public class ManagerController : Controller
{
    private readonly ILogger<ManagerController> _logger;
    private readonly IMetricsService _metricsService;
    private readonly IDispatchingService? _dispatchingService;
    private readonly IRankingService? _rankingService;
    private readonly IDispatchBacklogService? _dispatchBacklogService;
    private readonly ITicketService _ticketService;
    private readonly IProjectRepository _projectRepository;
    private readonly IAnticipationService? _anticipationService;
    private readonly ITicketGenerator _ticketGenerator;

    public ManagerController(
        ILogger<ManagerController> logger,
        IMetricsService metricsService,
        ITicketService ticketService,
        IProjectRepository projectRepository,
        ITicketGenerator ticketGenerator,
        IDispatchingService? dispatchingService = null,
        IRankingService? rankingService = null,
        IDispatchBacklogService? dispatchBacklogService = null,
        IAnticipationService? anticipationService = null)
    {
        _logger = logger;
        _metricsService = metricsService;
        _ticketService = ticketService;
        _projectRepository = projectRepository;
        _ticketGenerator = ticketGenerator;
        _dispatchingService = dispatchingService;
        _rankingService = rankingService;
        _dispatchBacklogService = dispatchBacklogService;
        _anticipationService = anticipationService;
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

            // Populate new analytics
            viewModel.ForecastData = await _metricsService.CalculateForecastAsync();
            viewModel.AgentPerformance = await _metricsService.CalculateClosedTicketsPerAgentAsync();

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
    /// Capacity Forecast showing anticipated inflow vs. capacity
    /// </summary>


    /// <summary>
    /// Capacity Forecast showing anticipated inflow vs. capacity
    /// </summary>
    public async Task<IActionResult> CapacityForecast(CancellationToken cancellationToken)
    {
        if (_anticipationService == null)
        {
            TempData["ErrorMessage"] = "GERDA Anticipation service is not available.";
            return RedirectToAction("TeamDashboard");
        }

        // Check for cancellation early
        cancellationToken.ThrowIfCancellationRequested();

        var forecast = await _anticipationService.CheckCapacityRiskAsync();

        // Get real forecast data (30 days)
        var forecastData = await _anticipationService.ForecastInflowAsync(30);
        var capacity = await _anticipationService.GetTeamCapacityAsync();

        var dates = new List<string>();
        var inflow = new List<int>();
        var capacityList = new List<int>();

        var today = DateTime.Today;

        if (forecastData.Count == 0)
        {
            // Fallback for empty state - show next 30 days with 0
            for (int i = 0; i < 30; i++)
            {
                dates.Add(today.AddDays(i).ToString("MMM dd"));
                inflow.Add(0);
                capacityList.Add((int)capacity);
            }
        }
        else
        {
            foreach (var item in forecastData)
            {
                dates.Add(item.Date.ToString("MMM dd"));
                inflow.Add(item.PredictedCount);
                capacityList.Add((int)capacity);
            }
        }

        ViewBag.Dates = JsonConvert.SerializeObject(dates);
        ViewBag.Inflow = JsonConvert.SerializeObject(inflow);
        ViewBag.Capacity = JsonConvert.SerializeObject(capacityList);
        ViewBag.RiskAnalysis = forecast;

        return View();
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
                    Comments = t.Comments?.Select(c => c.Body).ToList() ?? new List<string>(),
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
    public async Task<IActionResult> DispatchBacklog(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_dispatchBacklogService == null)
            {
                // Fallback for when GERDA configuration is missing
                return View(new GerdaDispatchViewModel
                {
                    Statistics = new DispatchStatistics(),
                    UnassignedTickets = new List<TicketDispatchInfo>(),
                    AvailableAgents = new List<AgentInfo>()
                });
            }

            _logger.LogInformation("Manager viewing GERDA Dispatch Backlog (Page {Page})", page);

            var viewModel = await _dispatchBacklogService.BuildDispatchBacklogViewModelAsync(page, pageSize, cancellationToken);

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
    /// Refactored to use TicketService (eliminates remaining _context usage)
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

            // Use TicketService with GERDA recommendation function
            var result = await _ticketService.BatchAssignTicketsAsync(
                request,
                async (ticketGuid) =>
                {
                    if (_dispatchingService?.IsEnabled == true)
                    {
                        return await _dispatchingService.GetRecommendedAgentAsync(ticketGuid);
                    }
                    return null;
                });

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
            if (_dispatchingService?.IsEnabled != true)
            {
                return Json(new { success = false, message = "GERDA Dispatching is disabled" });
            }

            var success = await _dispatchingService.AutoDispatchTicketAsync(ticketGuid);

            if (success)
            {
                var ticket = await _ticketService.GetTicketForEditAsync(ticketGuid);

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

    /// <summary>
    /// Manually trigger retraining of the GERDA dispatching model
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetrainDispatchModel()
    {
        try
        {
            if (_dispatchingService?.IsEnabled != true)
            {
                return Json(new { success = false, message = "GERDA Dispatching is disabled" });
            }

            _logger.LogInformation("Manually triggering dispatch model retraining...");
            await _dispatchingService.RetrainModelAsync();
            
            return Json(new { success = true, message = "Model retraining triggered successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retraining dispatch model");
            return Json(new { success = false, message = "Error retraining model: " + ex.Message });
        }
    }

    /// <summary>
    /// Manually trigger random ticket generation
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateRandomTicket()
    {
        try
        {
            await _ticketGenerator.GenerateRandomTicketAsync();
            return Json(new { success = true, message = "Random ticket generated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating random ticket manually");
            return Json(new { success = false, message = "Failed to generate ticket" });
        }
    }

    /// <summary>
    /// Trigger Golden Path data generation for demo
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateGoldenPath()
    {
        try
        {
            await _ticketGenerator.GenerateGoldenPathDataAsync();
            return Json(new { success = true, message = "Golden Path data generated successfully! Refresh page to see results." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Golden Path data");
            return Json(new { success = false, message = "Failed to generate Golden Path data: " + ex.Message });
        }
    }
}
