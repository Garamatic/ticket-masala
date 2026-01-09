using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Anticipation;
using Newtonsoft.Json;

namespace TicketMasala.Web.Controllers;

[Authorize(Roles = Constants.RoleAdmin)]
public class DashboardController : Controller
{
    private readonly ILogger<DashboardController> _logger;
    private readonly IMetricsService _metricsService;
    private readonly IAnticipationService? _anticipationService;

    public DashboardController(
        ILogger<DashboardController> logger,
        IMetricsService metricsService,
        IAnticipationService? anticipationService = null)
    {
        _logger = logger;
        _metricsService = metricsService;
        _anticipationService = anticipationService;
    }

    /// <summary>
    /// Team Dashboard showing GERDA AI metrics and team performance
    /// </summary>
    [HttpGet("Dashboard/Team")]
    public async Task<IActionResult> TeamDashboard()
    {
        try
        {
            _logger.LogInformation("Manager viewing Team Dashboard with GERDA metrics");

            var viewModel = await _metricsService.CalculateTeamMetricsAsync();

            // Populate new analytics
            viewModel.ForecastData = await _metricsService.CalculateForecastAsync();
            viewModel.AgentPerformance = await _metricsService.CalculateClosedTicketsPerAgentAsync();

            return View("~/Views/Manager/TeamDashboard.cshtml", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Team Dashboard");
            TempData["ErrorMessage"] = "Failed to load dashboard metrics.";
            return RedirectToAction("Index", "Home");
        }
    }

    /// <summary>
    /// Capacity Forecast showing anticipated inflow vs. capacity
    /// </summary>
    [HttpGet("Dashboard/Forecast")]
    public async Task<IActionResult> CapacityForecast(CancellationToken cancellationToken)
    {
        if (_anticipationService == null)
        {
            TempData["ErrorMessage"] = "GERDA Anticipation service is not available.";
            return RedirectToAction(nameof(TeamDashboard));
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

        return View("~/Views/Manager/CapacityForecast.cshtml");
    }
}
