using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IT_Project2526.Models;
using IT_Project2526.ViewModels;
using IT_Project2526.Services;
using IT_Project2526.Utilities;
using System.Diagnostics;
using System.Security.Claims;

namespace IT_Project2526.Controllers
{
    [Authorize(Roles = Constants.RoleEmployee + "," + Constants.RoleAdmin + "," + Constants.RoleCustomer)]
    public class ProjectsController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(
            IProjectService projectService,
            ILogger<ProjectsController> logger)
        {
            _projectService = projectService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isCustomer = User.IsInRole(Constants.RoleCustomer);

            var viewModels = await _projectService.GetAllProjectsAsync(userId, isCustomer);
            return View(viewModels.ToList());
        }

        [HttpGet]
        public async Task<IActionResult> NewProject()
        {
            try
            {
                _logger.LogInformation("New project form requested");

                var viewModel = new NewProject
                {
                    CustomerList = await _projectService.GetCustomerSelectListAsync(),
                    StakeholderList = await _projectService.GetStakeholderSelectListAsync(),
                    TemplateList = await _projectService.GetTemplateSelectListAsync(),
                    IsNewCustomer = false
                };

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

                if (ModelState.IsValid)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? throw new InvalidOperationException("User ID not found");

                    try
                    {
                        await _projectService.CreateProjectAsync(viewModel, userId);
                        return RedirectToAction("Index");
                    }
                    catch (InvalidOperationException ex)
                    {
                        ModelState.AddModelError(string.Empty, ex.Message);
                    }
                }

                viewModel.CustomerList = await _projectService.GetCustomerSelectListAsync();
                viewModel.StakeholderList = await _projectService.GetStakeholderSelectListAsync();
                viewModel.TemplateList = await _projectService.GetTemplateSelectListAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                var correlationId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
                _logger.LogError(ex, "Error creating project: {ProjectName}. CorrelationId: {CorrelationId}",
                    viewModel.Name, correlationId);

                ModelState.AddModelError(string.Empty, "An error occurred while creating the project. Please try again.");

                viewModel.CustomerList = await _projectService.GetCustomerSelectListAsync();
                viewModel.StakeholderList = await _projectService.GetStakeholderSelectListAsync();
                viewModel.TemplateList = await _projectService.GetTemplateSelectListAsync();
                return View(viewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var viewModel = await _projectService.GetProjectDetailsAsync(id);

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

                var viewModel = await _projectService.GetProjectForEditAsync(id);

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
                    viewModel.CustomerList = await _projectService.GetCustomerSelectListAsync();
                    return View(viewModel);
                }

                var success = await _projectService.UpdateProjectAsync(id, viewModel);

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

                viewModel.CustomerList = await _projectService.GetCustomerSelectListAsync();
                ModelState.AddModelError(string.Empty, "An error occurred while updating the project. Please try again.");
                return View(viewModel);
            }
        }
    }
}