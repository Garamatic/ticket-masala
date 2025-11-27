using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IT_Project2526.Models;
using IT_Project2526.ViewModels;
using IT_Project2526.Utilities;
using System.Diagnostics;
using System.Security.Claims;

namespace IT_Project2526.Controllers
{
    [Authorize(Roles = Constants.RoleEmployee + "," + Constants.RoleAdmin)]
    public class ProjectsController : Controller
    {
        private readonly ITProjectDB _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(
            ITProjectDB context,
            UserManager<ApplicationUser> userManager,
            ILogger<ProjectsController> logger)
        {
            _context = context;
            _userManager = userManager;
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
            var project = await _context.Projects
                .Include(p => p.ProjectManager)
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(m => m.Guid == guid);

            if (project == null) return NotFound();

            var viewModel = new ProjectDetailsViewModel
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

                    Customer? customer;

                    if (viewModel.IsNewCustomer)
                    {
                        customer = new Customer
                        {
                            FirstName = viewModel.NewCustomerFirstName ?? string.Empty,
                            LastName = viewModel.NewCustomerLastName ?? string.Empty,
                            Email = viewModel.NewCustomerEmail,
                            Phone = viewModel.NewCustomerPhone,
                            Code = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                            UserName = viewModel.NewCustomerEmail
                        };
                        
                        await _userManager.CreateAsync(customer);
                        await _userManager.AddToRoleAsync(customer, Constants.RoleCustomer);
                    }
                    else
                    {
                        customer = await _context.Customers
                            .FirstOrDefaultAsync(c => c.Id == viewModel.SelectedCustomerId);
                        
                        if (customer == null)
                        {
                            ModelState.AddModelError(string.Empty, "Selected customer not found");
                            viewModel.CustomerList = _context.Customers.Select(c => new SelectListItem
                            {
                                Value = c.Id.ToString(),
                                Text = c.Name
                            }).ToList();
                            return View(viewModel);
                        }
                    }

                    var project = new Project
                    {
                        Name = viewModel.Name,
                        Description = viewModel.Description,
                        Status = Status.Pending,
                        Customer = customer,
                        CompletionTarget = viewModel.CreationDate,
                        CreatorGuid = Guid.Parse(userId)
                    };

                    _context.Projects.Add(project);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Project created successfully: {ProjectId}", project.Guid);
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
                var project = await _context.Projects
                    .AsNoTracking()
                    .Include(p => p.Tasks)
                        .ThenInclude(t => t.Responsible)
                    .Include(p => p.Tasks)
                        .ThenInclude(t => t.Customer)
                    .Include(p => p.Customer)
                    .Include(p => p.ProjectManager)
                    .Include(p => p.Resources)
                    .Where(p => p.Guid == id && p.ValidUntil == null)
                    .FirstOrDefaultAsync();
                
                if (project == null)
                {
                    _logger.LogWarning("Project not found: {ProjectId}", id);
                    return NotFound();
                }

                var viewModel = new ProjectTicketViewModel
                {
                    ProjectDetails = new ProjectViewModel
                    {
                        Guid = project.Guid,
                        Name = project.Name,
                        Description = project.Description,
                        Status = project.Status,
                        ProjectManagerName = project.ProjectManager != null 
                            ? $"{project.ProjectManager.FirstName} {project.ProjectManager.LastName}" 
                            : "Not Assigned",
                        TicketCount = project.Tasks.Count
                    },
                    Tasks = project.Tasks.Select(t => new TicketViewModel
                    {
                        Guid = t.Guid,
                        Description = t.Description,
                        TicketStatus = t.TicketStatus,
                        ResponsibleName = t.Responsible != null 
                            ? $"{t.Responsible.FirstName} {t.Responsible.LastName}" 
                            : "Not Assigned",
                        CustomerName = t.Customer != null
                            ? $"{t.Customer.FirstName} {t.Customer.LastName}"
                            : "Unknown",
                        CompletionTarget = t.CompletionTarget,
                        CreationDate = DateTime.UtcNow
                    }).ToList()
                };
                
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

                var project = await _context.Projects
                    .Include(p => p.Customer)
                    .Where(p => p.Guid == id && p.ValidUntil == null)
                    .FirstOrDefaultAsync();

                if (project == null)
                {
                    _logger.LogWarning("Project not found for edit: {ProjectId}", id);
                    return NotFound();
                }

                var viewModel = new NewProject
                {
                    Guid = project.Guid,
                    Name = project.Name,
                    Description = project.Description,
                    SelectedCustomerId = project.Customer?.Id,
                    CreationDate = project.CompletionTarget,
                    IsNewCustomer = false,
                    CustomerList = _context.Customers.Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name,
                        Selected = project.Customer != null && c.Id == project.Customer.Id
                    }).ToList()
                };

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
                    viewModel.CustomerList = _context.Customers.Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    }).ToList();
                    return View(viewModel);
                }

                var project = await _context.Projects
                    .Include(p => p.Customer)
                    .Where(p => p.Guid == id && p.ValidUntil == null)
                    .FirstOrDefaultAsync();

                if (project == null)
                {
                    _logger.LogWarning("Project not found for update: {ProjectId}", id);
                    return NotFound();
                }

                project.Name = viewModel.Name;
                project.Description = viewModel.Description;
                project.CompletionTarget = viewModel.CreationDate;

                if (!string.IsNullOrEmpty(viewModel.SelectedCustomerId))
                {
                    var customer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.Id == viewModel.SelectedCustomerId);

                    if (customer != null)
                    {
                        project.Customer = customer;
                    }
                }

                _context.Projects.Update(project);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Project updated successfully: {ProjectId}", id);
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                var correlationId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
                _logger.LogError(ex, "Error updating project {ProjectId}. CorrelationId: {CorrelationId}", id, correlationId);
                
                viewModel.CustomerList = _context.Customers.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();
                
                ModelState.AddModelError(string.Empty, "An error occurred while updating the project. Please try again.");
                return View(viewModel);
            }
        }
    }
}