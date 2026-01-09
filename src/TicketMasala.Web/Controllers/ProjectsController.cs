using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.ViewModels.Projects;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Web.ViewModels.Customers;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Engine.Ingestion.Background;
using System.Diagnostics;
using System.Security.Claims;

namespace TicketMasala.Web.Controllers;

[Authorize(Roles = Constants.RoleEmployee + "," + Constants.RoleAdmin + "," + Constants.RoleCustomer)]
public class ProjectsController : Controller
{
    private readonly IProjectReadService _projectReadService;
    private readonly IProjectWorkflowService _projectWorkflowService;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        IProjectReadService projectReadService,
        IProjectWorkflowService projectWorkflowService,
        ILogger<ProjectsController> logger)
    {
        _projectReadService = projectReadService;
        _projectWorkflowService = projectWorkflowService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isCustomer = User.IsInRole(Constants.RoleCustomer);

        var viewModels = await _projectReadService.GetAllProjectsAsync(userId, isCustomer);
        return View(viewModels.ToList());
    }

    [HttpGet]
    public async Task<IActionResult> NewProject()
    {
        try
        {
            _logger.LogInformation("New project form requested");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isCustomer = User.IsInRole(Constants.RoleCustomer);

            var viewModel = new NewProject
            {
                StakeholderList = (await _projectReadService.GetStakeholderSelectListAsync()).ToList(),
                TemplateList = (await _projectReadService.GetTemplateSelectListAsync()).ToList(),
                ProjectManagerList = (await _projectReadService.GetEmployeeSelectListAsync()).Items.Cast<SelectListItem>().ToList(),
                IsNewCustomer = false
            };

            // Only show customer list for non-customer users
            if (!isCustomer)
            {
                viewModel.CustomerList = (await _projectReadService.GetCustomerSelectListAsync()).ToList();
            }
            else
            {
                // Pre-populate customer ID for customer users
                viewModel.SelectedCustomerId = userId;
                viewModel.IsNewCustomer = false; // Existing customer (themselves)
            }

            ViewBag.IsCustomer = isCustomer;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            var correlationId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError(ex, "Error loading new project form. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewProject(NewProject viewModel)
    {
        try
        {
            _logger.LogInformation("Attempting to create new project: {ProjectName}", viewModel.Name);

            // Customer authorization: customers can only create projects for themselves
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("User ID not found");
            var isCustomer = User.IsInRole(Constants.RoleCustomer);

            if (isCustomer)
            {
                // Override customer ID for customer users to prevent manipulation
                viewModel.SelectedCustomerId = userId;
                viewModel.IsNewCustomer = false;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _projectWorkflowService.CreateProjectAsync(viewModel, userId);
                    return RedirectToAction("Index");
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            viewModel.CustomerList = (await _projectReadService.GetCustomerSelectListAsync()).ToList();
            viewModel.StakeholderList = (await _projectReadService.GetStakeholderSelectListAsync()).ToList();
            viewModel.TemplateList = (await _projectReadService.GetTemplateSelectListAsync()).ToList();
            ViewBag.IsCustomer = isCustomer;
            return View(viewModel);
        }
        catch (Exception ex)
        {
            var correlationId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError(ex, "Error creating project: {ProjectName}. CorrelationId: {CorrelationId}",
                viewModel.Name, correlationId);

            ModelState.AddModelError(string.Empty, "An error occurred while creating the project. Please try again.");

            viewModel.CustomerList = (await _projectReadService.GetCustomerSelectListAsync()).ToList();
            viewModel.StakeholderList = (await _projectReadService.GetStakeholderSelectListAsync()).ToList();
            viewModel.TemplateList = (await _projectReadService.GetTemplateSelectListAsync()).ToList();
            return View(viewModel);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        try
        {
            var viewModel = await _projectReadService.GetProjectDetailsAsync(id);

            if (viewModel == null)
            {
                _logger.LogWarning("Project not found: {ProjectId}", id);
                return NotFound();
            }

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading project details: {ProjectId}", id);
            return StatusCode(500);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        try
        {
            _logger.LogInformation("Edit project form requested for project: {ProjectId}", id);

            var viewModel = await _projectReadService.GetProjectForEditAsync(id);

            if (viewModel == null)
            {
                _logger.LogWarning("Project not found for edit: {ProjectId}", id);
                return NotFound();
            }

            return View(viewModel);
        }
        catch (Exception ex)
        {
            var correlationId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError(ex, "Error loading edit project form for project {ProjectId}. CorrelationId: {CorrelationId}", id, correlationId);
            return StatusCode(500);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, NewProject viewModel)
    {
        try
        {
            _logger.LogInformation("Attempting to update project: {ProjectId}", id);

            if (id != viewModel.Guid)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                viewModel.CustomerList = (await _projectReadService.GetCustomerSelectListAsync()).ToList();
                viewModel.ProjectManagerList = (await _projectReadService.GetEmployeeSelectListAsync()).Items.Cast<SelectListItem>().ToList();
                return View(viewModel);
            }

            var success = await _projectWorkflowService.UpdateProjectAsync(id, viewModel);

            if (!success)
            {
                _logger.LogWarning("Project not found for update: {ProjectId}", id);
                return NotFound();
            }

            return RedirectToAction("Details", new { id });
        }
        catch (Exception ex)
        {
            var correlationId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError(ex, "Error updating project {ProjectId}. CorrelationId: {CorrelationId}", id, correlationId);

            viewModel.CustomerList = (await _projectReadService.GetCustomerSelectListAsync()).ToList();
            viewModel.ProjectManagerList = (await _projectReadService.GetEmployeeSelectListAsync()).Items.Cast<SelectListItem>().ToList();
            ModelState.AddModelError(string.Empty, "An error occurred while updating the project. Please try again.");
            return View(viewModel);
        }
    }

    /// <summary>
    /// Create a new project from an existing ticket
    /// Shows form with pre-filled data and GERDA PM recommendation
    /// </summary>
    [HttpGet]
    [Authorize(Roles = Constants.RoleEmployee + "," + Constants.RoleAdmin)]
    public async Task<IActionResult> CreateFromTicket(Guid ticketId)
    {
        try
        {
            _logger.LogInformation("Create project from ticket requested for ticket: {TicketId}", ticketId);

            // Check if ticket already belongs to a project
            var existingProjectId = await _projectReadService.GetProjectIdForTicketAsync(ticketId);
            if (existingProjectId.HasValue)
            {
                TempData["Warning"] = "This ticket already belongs to a project.";
                return RedirectToAction("Details", new { id = existingProjectId.Value });
            }

            var viewModel = await _projectReadService.PrepareCreateFromTicketViewModelAsync(ticketId);

            if (viewModel == null)
            {
                _logger.LogWarning("Ticket not found: {TicketId}", ticketId);
                return NotFound();
            }

            return View(viewModel);
        }
        catch (Exception ex)
        {
            var correlationId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError(ex, "Error loading create from ticket form for ticket {TicketId}. CorrelationId: {CorrelationId}", ticketId, correlationId);
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Create a new project from an existing ticket (POST)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Constants.RoleEmployee + "," + Constants.RoleAdmin)]
    public async Task<IActionResult> CreateFromTicket(CreateProjectFromTicketViewModel viewModel)
    {
        try
        {
            _logger.LogInformation("Attempting to create project from ticket: {TicketId}", viewModel.TicketId);

            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? throw new InvalidOperationException("User ID not found");

                var projectId = await _projectWorkflowService.CreateProjectFromTicketAsync(viewModel, userId);

                if (projectId.HasValue)
                {
                    return RedirectToAction("Details", new { id = projectId.Value });
                }

                ModelState.AddModelError(string.Empty, "Failed to create project from ticket.");
            }

            // Reload select lists
            viewModel.TemplateList = new SelectList(await _projectReadService.GetTemplateSelectListAsync(), "Value", "Text");
            viewModel.ProjectManagerList = await _projectReadService.GetEmployeeSelectListAsync();
            return View(viewModel);
        }
        catch (Exception ex)
        {
            var correlationId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError(ex, "Error creating project from ticket: {TicketId}. CorrelationId: {CorrelationId}",
                viewModel.TicketId, correlationId);

            ModelState.AddModelError(string.Empty, "An error occurred while creating the project. Please try again.");

            viewModel.TemplateList = new SelectList(await _projectReadService.GetTemplateSelectListAsync(), "Value", "Text");
            viewModel.ProjectManagerList = await _projectReadService.GetEmployeeSelectListAsync();
            return View(viewModel);
        }
    }

    /// <summary>
    /// Delete a project (Admin only)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Constants.RoleAdmin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            _logger.LogInformation("Attempting to delete project: {ProjectId}", id);

            var success = await _projectWorkflowService.DeleteProjectAsync(id);

            if (!success)
            {
                _logger.LogWarning("Project not found for deletion: {ProjectId}", id);
                TempData["Error"] = "Project not found.";
                return RedirectToAction("Index");
            }

            TempData["Success"] = "Project deleted successfully.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            var correlationId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError(ex, "Error deleting project {ProjectId}. CorrelationId: {CorrelationId}", id, correlationId);

            TempData["Error"] = "An error occurred while deleting the project. The project may have associated tickets.";
            return RedirectToAction("Details", new { id });
        }
    }
}
