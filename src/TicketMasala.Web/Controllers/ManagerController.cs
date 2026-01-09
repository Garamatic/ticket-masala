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
    private readonly IProjectRepository _projectRepository;
    private readonly ITicketGenerator _ticketGenerator;

    public ManagerController(
        ILogger<ManagerController> logger,
        IProjectRepository projectRepository,
        ITicketGenerator ticketGenerator)
    {
        _logger = logger;
        _projectRepository = projectRepository;
        _ticketGenerator = ticketGenerator;
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
