using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.ViewModels.Projects;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Data;
using System.Security.Claims;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.AI;

namespace TicketMasala.Web.Controllers.Api;

/// <summary>
/// REST API for Project management - designed for AI microservice integration
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/projects")]
[Authorize(Roles = Constants.RoleEmployee + "," + Constants.RoleAdmin)]
[Produces("application/json")]
public class ProjectsApiController : ControllerBase
{
    private readonly IProjectReadService _projectReadService;
    private readonly IProjectWorkflowService _projectWorkflowService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ProjectsApiController> _logger;
    private readonly IOpenAiService _openAiService;

    public ProjectsApiController(
        IProjectReadService projectReadService,
        IProjectWorkflowService projectWorkflowService,
        UserManager<ApplicationUser> userManager,
        ILogger<ProjectsApiController> logger,
        IOpenAiService openAiService)
    {
        _projectReadService = projectReadService;
        _projectWorkflowService = projectWorkflowService;
        _userManager = userManager;
        _logger = logger;
        _openAiService = openAiService;
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

            var projects = await _projectReadService.GetAllProjectsAsync(null, false);

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
            var viewModel = await _projectReadService.GetProjectDetailsAsync(id);

            if (viewModel == null)
            {
                _logger.LogWarning("API: Project {ProjectId} not found", id);
                return NotFound(ApiResponse<ProjectTicketViewModel>.ErrorResponse(
                    $"Project with ID {id} not found"));
            }

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
            var projects = await _projectReadService.GetProjectsByCustomerAsync(customerId);

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
            var projects = await _projectReadService.SearchProjectsAsync(query);

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
    [ProducesResponseType(typeof(ApiResponse<ProjectStatisticsViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ProjectStatisticsViewModel>>> GetStatistics(string customerId)
    {
        try
        {
            var stats = await _projectReadService.GetProjectStatisticsAsync(customerId);

            return Ok(ApiResponse<ProjectStatisticsViewModel>.SuccessResponse(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error getting statistics for customer {CustomerId}", customerId);
            return StatusCode(500, ApiResponse<ProjectStatisticsViewModel>.ErrorResponse(
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

            var project = await _projectWorkflowService.CreateProjectAsync(model, userId);

            return CreatedAtAction(
                nameof(GetById),
                new { id = project.Guid },
                ApiResponse<Guid>.SuccessResponse(project.Guid, "Project created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<Guid>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error creating project");
            return StatusCode(500, ApiResponse<Guid>.ErrorResponse(
                "An error occurred while creating the project"));
        }
    }

    /// <summary>
    /// Generate AI roadmap for project description
    /// </summary>
    /// <param name="description">Project description to generate roadmap for</param>
    /// <returns>AI-generated roadmap steps</returns>
    [HttpPost("generate-roadmap")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> GenerateRoadmap([FromBody] string description)
    {
        try
        {
            var roadmap = await _openAiService.GetResponseAsync(OpenAIPrompts.Steps, description);
            return Ok(ApiResponse<string>.SuccessResponse(roadmap, "Roadmap generated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error generating AI roadmap");
            return StatusCode(500, ApiResponse<string>.ErrorResponse(
                "An error occurred while generating the roadmap"));
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
            var success = await _projectWorkflowService.UpdateProjectStatusAsync(id, status);

            if (!success)
            {
                return NotFound(ApiResponse<string>.ErrorResponse($"Project {id} not found"));
            }

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
            var success = await _projectWorkflowService.AssignProjectManagerAsync(id, managerId);

            if (!success)
            {
                return NotFound(ApiResponse<string>.ErrorResponse($"Project {id} or Manager {managerId} not found"));
            }

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
            var success = await _projectWorkflowService.DeleteProjectAsync(id);

            if (success)
            {
                return Ok(ApiResponse<string>.SuccessResponse(
                    id.ToString(),
                    "Project deleted successfully"));
            }
            else
            {
                // Return success even if not found? Or NotFound? 
                // Usually idempotency says OK, but here assuming explicit delete intention.
                // But since its soft delete and we return "deleted successfully" even if already deleted?
                // I'll stick to logic: if not found/not deleted, maybe return NotFound?
                // Original logic: "if project != null ... await SaveChanges ... if project == null do nothing"
                // The original impl always returned OK.
                return Ok(ApiResponse<string>.SuccessResponse(
                   id.ToString(),
                   "Project deleted successfully"));
            }
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
