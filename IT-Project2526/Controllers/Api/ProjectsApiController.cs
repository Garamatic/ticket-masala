using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IT_Project2526.ViewModels;
using IT_Project2526.Utilities;
using IT_Project2526.Models;
using System.Security.Claims;

namespace IT_Project2526.Controllers.Api
{
    /// <summary>
    /// REST API for Project management - designed for AI microservice integration
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Roles = Constants.RoleEmployee + "," + Constants.RoleAdmin)]
    [Produces("application/json")]
    public class ProjectsApiController : ControllerBase
    {
        private readonly ITProjectDB _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ProjectsApiController> _logger;

        public ProjectsApiController(
            ITProjectDB context,
            UserManager<ApplicationUser> userManager,
            ILogger<ProjectsApiController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Get all projects with their tasks
        /// </summary>
        /// <returns>List of projects with task details</returns>
        /// <response code="200">Returns the list of projects</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="403">If the user doesn't have permission</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProjectTicketViewModel>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectTicketViewModel>>>> GetAll()
        {
            try
            {
                _logger.LogInformation("API: Getting all projects");
                
                var projects = await _context.Projects
                    .AsNoTracking()
                    .Include(p => p.Tasks)
                        .ThenInclude(t => t.Responsible)
                    .Include(p => p.Tasks)
                        .ThenInclude(t => t.Customer)
                    .Include(p => p.Customer)
                    .Include(p => p.ProjectManager)
                    .Where(p => p.ValidUntil == null)
                    .Select(p => new ProjectTicketViewModel
                    {
                        ProjectDetails = new ProjectViewModel
                        {
                            Guid = p.Guid,
                            Name = p.Name,
                            Description = p.Description,
                            Status = p.Status,
                            ProjectManagerName = p.ProjectManager != null 
                                ? $"{p.ProjectManager.FirstName} {p.ProjectManager.LastName}" 
                                : "Not Assigned",
                            TicketCount = p.Tasks.Count
                        },
                        Tasks = p.Tasks.Select(t => new TicketViewModel
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
                    })
                    .ToListAsync();

                return Ok(ApiResponse<IEnumerable<ProjectTicketViewModel>>.SuccessResponse(
                    projects, 
                    $"Retrieved {projects.Count()} projects"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error getting all projects");
                return StatusCode(500, ApiResponse<IEnumerable<ProjectTicketViewModel>>.ErrorResponse(
                    "An error occurred while retrieving projects"));
            }
        }

        /// <summary>
        /// Get a specific project by ID
        /// </summary>
        /// <param name="id">The project GUID</param>
        /// <returns>Project with full details</returns>
        /// <response code="200">Returns the project</response>
        /// <response code="404">If the project is not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ProjectTicketViewModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ProjectTicketViewModel>>> GetById(Guid id)
        {
            try
            {
                var project = await _context.Projects
                    .AsNoTracking()
                    .Include(p => p.Tasks.Where(t => t.ValidUntil == null))
                        .ThenInclude(t => t.Responsible)
                    .Include(p => p.Tasks.Where(t => t.ValidUntil == null))
                        .ThenInclude(t => t.Customer)
                    .Include(p => p.Customer)
                    .Include(p => p.ProjectManager)
                    .Where(p => p.Guid == id && p.ValidUntil == null)
                    .FirstOrDefaultAsync();
                
                if (project == null)
                {
                    _logger.LogWarning("API: Project {ProjectId} not found", id);
                    return NotFound(ApiResponse<ProjectTicketViewModel>.ErrorResponse(
                        $"Project with ID {id} not found"));
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

                return Ok(ApiResponse<ProjectTicketViewModel>.SuccessResponse(viewModel));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error getting project {ProjectId}", id);
                return StatusCode(500, ApiResponse<ProjectTicketViewModel>.ErrorResponse(
                    "An error occurred while retrieving the project"));
            }
        }

        /// <summary>
        /// Get all projects for a specific customer
        /// </summary>
        /// <param name="customerId">The customer ID</param>
        /// <returns>List of customer's projects</returns>
        [HttpGet("customer/{customerId}")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProjectTicketViewModel>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectTicketViewModel>>>> GetByCustomer(string customerId)
        {
            try
            {
                var projects = await _context.Projects
                    .AsNoTracking()
                    .Include(p => p.Tasks.Where(t => t.ValidUntil == null))
                        .ThenInclude(t => t.Responsible)
                    .Include(p => p.Tasks.Where(t => t.ValidUntil == null))
                        .ThenInclude(t => t.Customer)
                    .Include(p => p.Customer)
                    .Include(p => p.ProjectManager)
                    .Where(p => p.CustomerId == customerId && p.ValidUntil == null)
                    .Select(p => new ProjectTicketViewModel
                    {
                        ProjectDetails = new ProjectViewModel
                        {
                            Guid = p.Guid,
                            Name = p.Name,
                            Description = p.Description,
                            Status = p.Status,
                            ProjectManagerName = p.ProjectManager != null 
                                ? $"{p.ProjectManager.FirstName} {p.ProjectManager.LastName}" 
                                : "Not Assigned",
                            TicketCount = p.Tasks.Count
                        },
                        Tasks = p.Tasks.Select(t => new TicketViewModel
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
                    })
                    .ToListAsync();

                return Ok(ApiResponse<IEnumerable<ProjectTicketViewModel>>.SuccessResponse(
                    projects,
                    $"Retrieved {projects.Count()} projects for customer {customerId}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error getting projects for customer {CustomerId}", customerId);
                return StatusCode(500, ApiResponse<IEnumerable<ProjectTicketViewModel>>.ErrorResponse(
                    "An error occurred while retrieving customer projects"));
            }
        }

        /// <summary>
        /// Search projects by name
        /// </summary>
        /// <param name="query">Search term</param>
        /// <returns>Matching projects</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProjectTicketViewModel>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectTicketViewModel>>>> Search([FromQuery] string query)
        {
            try
            {
                var projects = await _context.Projects
                    .AsNoTracking()
                    .Include(p => p.Tasks.Where(t => t.ValidUntil == null))
                        .ThenInclude(t => t.Responsible)
                    .Include(p => p.Tasks.Where(t => t.ValidUntil == null))
                        .ThenInclude(t => t.Customer)
                    .Include(p => p.Customer)
                    .Include(p => p.ProjectManager)
                    .Where(p => p.ValidUntil == null && 
                        (p.Name.Contains(query) || p.Description.Contains(query)))
                    .Select(p => new ProjectTicketViewModel
                    {
                        ProjectDetails = new ProjectViewModel
                        {
                            Guid = p.Guid,
                            Name = p.Name,
                            Description = p.Description,
                            Status = p.Status,
                            ProjectManagerName = p.ProjectManager != null 
                                ? $"{p.ProjectManager.FirstName} {p.ProjectManager.LastName}" 
                                : "Not Assigned",
                            TicketCount = p.Tasks.Count
                        },
                        Tasks = p.Tasks.Select(t => new TicketViewModel
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
                    })
                    .ToListAsync();

                return Ok(ApiResponse<IEnumerable<ProjectTicketViewModel>>.SuccessResponse(
                    projects,
                    $"Found {projects.Count()} projects matching '{query}'"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error searching projects with query {Query}", query);
                return StatusCode(500, ApiResponse<IEnumerable<ProjectTicketViewModel>>.ErrorResponse(
                    "An error occurred while searching projects"));
            }
        }

        /// <summary>
        /// Get customer statistics
        /// </summary>
        /// <param name="customerId">The customer ID</param>
        /// <returns>Project statistics for the customer</returns>
        [HttpGet("statistics/{customerId}")]
        [ProducesResponseType(typeof(ApiResponse<ProjectStatistics>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<ProjectStatistics>>> GetStatistics(string customerId)
        {
            try
            {
                var projects = await _context.Projects
                    .AsNoTracking()
                    .Include(p => p.Tasks)
                    .Where(p => p.CustomerId == customerId && p.ValidUntil == null)
                    .ToListAsync();

                var stats = new ProjectStatistics
                {
                    TotalProjects = projects.Count,
                    ActiveProjects = projects.Count(p => p.Status == Status.InProgress),
                    CompletedProjects = projects.Count(p => p.Status == Status.Completed),
                    PendingProjects = projects.Count(p => p.Status == Status.Pending),
                    TotalTasks = projects.Sum(p => p.Tasks.Count),
                    CompletedTasks = projects.Sum(p => p.Tasks.Count(t => t.TicketStatus == Status.Completed))
                };

                return Ok(ApiResponse<ProjectStatistics>.SuccessResponse(stats));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error getting statistics for customer {CustomerId}", customerId);
                return StatusCode(500, ApiResponse<ProjectStatistics>.ErrorResponse(
                    "An error occurred while retrieving statistics"));
            }
        }

        /// <summary>
        /// Create a new project
        /// </summary>
        /// <param name="model">Project creation data</param>
        /// <returns>Created project ID</returns>
        /// <response code="201">Project created successfully</response>
        /// <response code="400">If the model is invalid</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] NewProject model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                    
                return BadRequest(ApiResponse<Guid>.ErrorResponse(errors));
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? throw new UnauthorizedAccessException("User ID not found");

                Customer? customer;

                if (model.IsNewCustomer)
                {
                    customer = new Customer
                    {
                        FirstName = model.NewCustomerFirstName ?? string.Empty,
                        LastName = model.NewCustomerLastName ?? string.Empty,
                        Email = model.NewCustomerEmail,
                        Phone = model.NewCustomerPhone,
                        Code = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                        UserName = model.NewCustomerEmail
                    };
                    
                    await _userManager.CreateAsync(customer);
                    await _userManager.AddToRoleAsync(customer, Constants.RoleCustomer);
                }
                else
                {
                    customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == model.SelectedCustomerId);
                    if (customer == null)
                    {
                        return BadRequest(ApiResponse<Guid>.ErrorResponse("Selected customer not found"));
                    }
                }

                var project = new Project
                {
                    Name = model.Name,
                    Description = model.Description,
                    Status = Status.Pending,
                    Customer = customer,
                    CompletionTarget = model.CreationDate,
                    CreatorGuid = Guid.Parse(userId)
                };

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                _logger.LogInformation("API: Project created {ProjectId}", project.Guid);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = project.Guid },
                    ApiResponse<Guid>.SuccessResponse(project.Guid, "Project created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error creating project");
                return StatusCode(500, ApiResponse<Guid>.ErrorResponse(
                    "An error occurred while creating the project"));
            }
        }

        /// <summary>
        /// Update project status
        /// </summary>
        /// <param name="id">Project ID</param>
        /// <param name="status">New status</param>
        /// <returns>Success response</returns>
        [HttpPatch("{id}/status")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<string>>> UpdateStatus(Guid id, [FromBody] Status status)
        {
            try
            {
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.Guid == id && p.ValidUntil == null);
                
                if (project == null)
                {
                    return NotFound(ApiResponse<string>.ErrorResponse($"Project {id} not found"));
                }

                project.Status = status;
                if (status == Status.Completed)
                {
                    project.CompletionDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<string>.SuccessResponse(
                    status.ToString(),
                    $"Project status updated to {status}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error updating project status {ProjectId}", id);
                return StatusCode(500, ApiResponse<string>.ErrorResponse(
                    "An error occurred while updating project status"));
            }
        }

        /// <summary>
        /// Assign a project manager
        /// </summary>
        /// <param name="id">Project ID</param>
        /// <param name="managerId">Manager user ID</param>
        /// <returns>Success response</returns>
        [HttpPatch("{id}/assign-manager")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<string>>> AssignManager(Guid id, [FromBody] string managerId)
        {
            try
            {
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.Guid == id && p.ValidUntil == null);
                if (project == null)
                {
                    return NotFound(ApiResponse<string>.ErrorResponse($"Project {id} not found"));
                }

                var manager = await _userManager.FindByIdAsync(managerId) as Employee;
                if (manager == null)
                {
                    return NotFound(ApiResponse<string>.ErrorResponse($"Manager {managerId} not found"));
                }

                project.ProjectManager = manager;
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<string>.SuccessResponse(
                    managerId,
                    "Project manager assigned successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error assigning manager to project {ProjectId}", id);
                return StatusCode(500, ApiResponse<string>.ErrorResponse(
                    "An error occurred while assigning the project manager"));
            }
        }

        /// <summary>
        /// Delete a project (soft delete)
        /// </summary>
        /// <param name="id">Project ID</param>
        /// <returns>Success response</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = Constants.RoleAdmin)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id)
        {
            try
            {
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.Guid == id);
                if (project != null)
                {
                    project.ValidUntil = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return Ok(ApiResponse<string>.SuccessResponse(
                    id.ToString(),
                    "Project deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error deleting project {ProjectId}", id);
                return StatusCode(500, ApiResponse<string>.ErrorResponse(
                    "An error occurred while deleting the project"));
            }
        }
    }

    /// <summary>
    /// Standard API response wrapper
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResponse<T> SuccessResponse(T data, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        public static ApiResponse<T> ErrorResponse(string error)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Errors = new List<string> { error }
            };
        }

        public static ApiResponse<T> ErrorResponse(List<string> errors)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Errors = errors
            };
        }
    }

    /// <summary>
    /// Project statistics model
    /// </summary>
    public class ProjectStatistics
    {
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public int PendingProjects { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
    }
}
