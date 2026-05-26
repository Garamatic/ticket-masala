using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Engine.GERDA.Dispatching;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;
using TicketMasala.Web.ViewModels.Dashboard; // Check if needed
using TicketMasala.Web.ViewModels.GERDA;
using TicketMasala.Web.ViewModels.Tickets; // Check if needed

namespace TicketMasala.Web.Controllers;

[Authorize(Roles = Constants.RoleAdmin)]
public class DispatchController : Controller
{
    private readonly ILogger<DispatchController> _logger;
    private readonly ITicketDispatcher? _dispatcher;
    private readonly IDispatchBacklogService? _dispatchBacklogService;
    private readonly ITicketReadService _ticketReadService;
    private readonly ITicketLifecycle _ticketLifecycle;
    private readonly ITicketBatchService _ticketBatchService;

    public DispatchController(
        ILogger<DispatchController> logger,
        ITicketReadService ticketReadService,
        ITicketLifecycle ticketLifecycle,
        ITicketBatchService ticketBatchService,
        ITicketDispatcher? dispatcher = null,
        IDispatchBacklogService? dispatchBacklogService = null)
    {
        _logger = logger;
        _ticketReadService = ticketReadService;
        _ticketLifecycle = ticketLifecycle;
        _ticketBatchService = ticketBatchService;
        _dispatcher = dispatcher;
        _dispatchBacklogService = dispatchBacklogService;
    }

    /// <summary>
    /// GERDA Dispatching Dashboard - Shows backlog with AI recommendations
    /// </summary>
    [HttpGet("Dispatch/Backlog")]
    public async Task<IActionResult> DispatchBacklog(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_dispatchBacklogService == null)
            {
                // Fallback for when GERDA configuration is missing
                return View("~/Views/Dispatch/DispatchBacklog.cshtml", new GerdaDispatchViewModel
                {
                    Statistics = new DispatchStatistics(),
                    UnassignedTickets = new List<TicketDispatchInfo>(),
                    AvailableAgents = new List<AgentInfo>()
                });
            }

            _logger.LogInformation("Manager viewing GERDA Dispatch Backlog (Page {Page})", page);

            var viewModel = await _dispatchBacklogService.BuildDispatchBacklogViewModelAsync(page, pageSize, cancellationToken);

            return View("~/Views/Dispatch/DispatchBacklog.cshtml", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading GERDA Dispatch Backlog");
            TempData["ErrorMessage"] = "Failed to load dispatch backlog.";
            return RedirectToAction("Index", "Home");
        }
    }

    /// <summary>
    /// Assign a single ticket to an agent and/or project
    /// </summary>
    [HttpPost("Dispatch/AssignTicket")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTicket(Guid ticketGuid, string? agentId, Guid? projectGuid)
    {
        try
        {
            var assignResult = await _ticketLifecycle.ExecuteAsync(
                new AssignTicketCommand(ticketGuid, agentId, projectGuid),
                new TicketContext("system"));

            var success = assignResult.Success;

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
    [HttpPost("Dispatch/BatchAssign")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchAssignTickets([FromBody] BatchAssignRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Manager batch assigning {Count} tickets, UseGerda={UseGerda}",
                request.TicketGuids.Count, request.UseGerdaRecommendations);

            // Use TicketBatchService with GERDA recommendation function
            var result = await _ticketBatchService.BatchAssignTicketsAsync(
                request,
                async (ticketGuid) =>
                {
                    if (_dispatcher?.IsEnabled != true)
                        return null;

                    var dispatchResult = await _dispatcher.ExecuteAsync(
                        new RecommendAgentsCommand(ticketGuid, 1));

                    return dispatchResult.Success && dispatchResult.Recommendations.Count > 0
                        ? dispatchResult.Recommendations[0].AgentId
                        : null;
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
    [HttpPost("Dispatch/AutoDispatch")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AutoDispatchTicket(Guid ticketGuid)
    {
        try
        {
            if (_dispatcher?.IsEnabled != true)
            {
                return Json(new { success = false, message = "GERDA Dispatching is disabled" });
            }

            var dispatchResult = await _dispatcher.ExecuteAsync(
                new AutoDispatchCommand(ticketGuid));

            if (dispatchResult.Success && dispatchResult.WasAutoAssigned)
            {
                var ticket = await _ticketReadService.GetTicketForEditAsync(ticketGuid);

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
    [HttpPost("Dispatch/Retrain")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetrainDispatchModel()
    {
        try
        {
            if (_dispatcher?.IsEnabled != true)
            {
                return Json(new { success = false, message = "GERDA Dispatching is disabled" });
            }

            _logger.LogInformation("Manually triggering dispatch model retraining...");
            var retrainResult = await _dispatcher.ExecuteAsync(new RetrainCommand());

            return Json(new
            {
                success = retrainResult.Success,
                message = retrainResult.Success
                    ? "Model retraining triggered successfully."
                    : (retrainResult.ErrorMessage ?? "Retraining failed")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retraining dispatch model");
            return Json(new { success = false, message = "Error retraining model: " + ex.Message });
        }
    }
}
