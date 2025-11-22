using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using IT_Project2526.Models;
using IT_Project2526.ViewModels;
using IT_Project2526.Utilities;
using IT_Project2526.Services;
using System.Diagnostics;
using System.Security.Claims;

namespace IT_Project2526.Controllers
{
    [Authorize(Roles = Constants.RoleEmployee + "," + Constants.RoleAdmin)]
    public class ProjectsController : Controller
    {
        private readonly ITProjectDB _context;
        private readonly IProjectService _projectService;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(
            ITProjectDB context, 
            IProjectService projectService,
            ILogger<ProjectsController> logger)
        {
            _context = context;
            _projectService = projectService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            //Projecten uit Db halen met hun tickets
            var projectsOfDb = _context.Projects
                                       .Include(p => p.Tasks)
                                       .Include(p => p.ProjectManager)
                                       .ToList();

            //Models naar ViewModels
            List<ProjectTicketViewModel> viewModels = projectsOfDb.Select(p => new ProjectTicketViewModel
            {
                ProjectDetails = new ProjectViewModel
                {
                    Name = p.Name,
                    Description = p.Description,
                    Status = p.Status,
                    ProjectManager = p.ProjectManager,
                },
                Tasks = p.Tasks.Select(t => new TicketViewModel
                {
                    Guid = t.Guid,
                    TicketStatus = t.TicketStatus,
                    CreationDate = t.CreationDate,
                }).ToList()
            }).ToList();

            return View(viewModels);
        }

        [HttpGet]
        public IActionResult NewProject()
        {
            try
            {
                _logger.LogInformation("New project form requested");
                
                var existingCustomers = _context.Customers.ToList();
                var viewModel = new NewProject
                {
                    CustomerList = existingCustomers.Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    }).ToList(),
                    IsNewCustomer = true
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

                    var projectId = await _projectService.CreateProjectAsync(viewModel, userId);
                    
                    _logger.LogInformation("Project created successfully: {ProjectId}", projectId);
                    return RedirectToAction("Index");
                }

                viewModel.CustomerList = _context.Customers.ToList().Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                var correlationId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
                _logger.LogError(ex, "Error creating project: {ProjectName}. CorrelationId: {CorrelationId}", 
                    viewModel.Name, correlationId);
                
                ModelState.AddModelError(string.Empty, "An error occurred while creating the project. Please try again.");
                
                viewModel.CustomerList = _context.Customers.ToList().Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();
                
                return View(viewModel);
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var project = await _projectService.GetProjectByIdAsync(id);
                
                if (project == null)
                {
                    _logger.LogWarning("Project not found: {ProjectId}", id);
                    return NotFound();
                }
                
                return View(project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading project details: {ProjectId}", id);
                return StatusCode(500);
            }
        }
    }
}