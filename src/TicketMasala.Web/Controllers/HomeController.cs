using System.Diagnostics;
using TicketMasala.Web.ViewModels.Shared;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TicketMasala.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ITicketService _ticketService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HomeController(
        ILogger<HomeController> logger,
        ITicketService ticketService,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _ticketService = ticketService;
        _httpContextAccessor = httpContextAccessor;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        // If user is authenticated, fetch dynamic stats
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        try
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isCustomer = User.IsInRole(TicketMasala.Domain.Common.Constants.RoleCustomer);

            var stats = await _ticketService.GetDashboardStatsAsync(userId, isCustomer);
            ViewBag.ProjectCount = stats.ProjectCount;
            ViewBag.ActiveTicketCount = stats.ActiveTicketCount;
            ViewBag.PendingTaskCount = stats.PendingTaskCount;
            ViewBag.CompletionRate = stats.CompletionRate;
            ViewBag.NewProjectsThisWeek = stats.NewProjectsThisWeek;
            ViewBag.CompletedToday = stats.CompletedToday;
            ViewBag.DueSoon = stats.DueSoon;
            ViewBag.HighRiskCount = stats.HighRiskCount;
            ViewBag.SentimentWarningCount = stats.SentimentWarningCount;

            // Fetch Recent Activity
            ViewBag.RecentActivity = await _ticketService.GetRecentActivityAsync(userId, 3);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch dashboard stats");
            // Fallback to defaults
            ViewBag.ProjectCount = 0;
            ViewBag.ActiveTicketCount = 0;
            ViewBag.PendingTaskCount = 0;
            ViewBag.CompletionRate = 0;
            ViewBag.RecentActivity = new List<TicketMasala.Web.ViewModels.Tickets.TicketViewModel>();
        }

        return View();
    }

    [AllowAnonymous]
    public IActionResult Demo()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // Get error details
        var exceptionHandlerPathFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var errorMessage = "An unexpected error occurred.";
        var statusCode = 500;

        if (exceptionHandlerPathFeature?.Error != null)
        {
            var exception = exceptionHandlerPathFeature.Error;

            // Log the exception (if not already logged by middleware)
            _logger.LogError(exception, "Unhandled exception encountered");

            if (exception is UnauthorizedAccessException)
            {
                errorMessage = "You do not have permission to access this resource.";
                statusCode = 403;
            }
            else if (exception is KeyNotFoundException)
            {
                errorMessage = "The requested resource could not be found.";
                statusCode = 404;
            }
            else if (exception is OperationCanceledException)
            {
                errorMessage = "The operation was cancelled.";
                statusCode = 499; // Client Closed Request
            }
        }

        Response.StatusCode = statusCode;

        var viewModel = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
        };

        // Pass error message to view (using ViewData to avoid modifying ViewModel definition if possible, 
        // or we could extend the view model)
        ViewData["ErrorMessage"] = errorMessage;
        ViewData["StatusCode"] = statusCode;

        return View(viewModel);
    }
}

