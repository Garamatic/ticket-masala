using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.ViewModels.Api;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Extensions;
using TicketMasala.Web.ViewModels.Projects;
using TicketMasala.Domain.Common;

namespace TicketMasala.Web.Controllers.Api.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/work-containers")]
[ApiController]
public class WorkContainersController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<WorkContainersController> _logger;

    public WorkContainersController(
        IProjectService projectService,
        IProjectRepository projectRepository,
        ILogger<WorkContainersController> logger)
    {
        _projectService = projectService;
        _projectRepository = projectRepository;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WorkContainerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var projects = await _projectRepository.GetAllAsync();
        return Ok(projects.Select(p => p.ToWorkContainerDto(p.Tasks?.Count ?? 0)));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WorkContainerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var project = await _projectRepository.GetByIdAsync(id, includeRelations: true);
            if (project == null)
                return NotFound();

            return Ok(project.ToWorkContainerDto(project.Tasks?.Count ?? 0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving work container {Id}", id);
            return NotFound();
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(WorkContainerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(WorkContainerDto container)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = container.ManagerId ?? "SYSTEM";

            var vm = new NewProject
            {
                Name = container.Name,
                Description = container.Description ?? "",
                SelectedCustomerId = container.CustomerId, // Fixed: mapped to correct property
                IsNewCustomer = false, // Fixed: Explicitly set to false for existing customer
                ProjectType = container.ProjectType // Map if possible
            };

            var project = await _projectService.CreateProjectAsync(vm, userId);

            // Post-creation update for extra fields not in ViewModel
            bool needsUpdate = false;
            if (container.Status != null && Enum.TryParse<Status>(container.Status, true, out var status) && project.Status != status)
            {
                project.Status = status;
                needsUpdate = true;
            }
            if (container.Notes != null && project.Notes != container.Notes)
            {
                project.Notes = container.Notes;
                needsUpdate = true;
            }
            if (container.AiRoadmap != null && project.ProjectAiRoadmap != container.AiRoadmap)
            {
                project.ProjectAiRoadmap = container.AiRoadmap;
                needsUpdate = true;
            }
            // Handle CompletionTarget manually
            if (container.CompletionTarget.HasValue && project.CompletionTarget != container.CompletionTarget)
            {
                project.CompletionTarget = container.CompletionTarget.Value;
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                await _projectRepository.UpdateAsync(project);
            }

            return CreatedAtAction(nameof(GetById), new { id = project.Guid, version = "1.0" }, project.ToWorkContainerDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating work container");
            return StatusCode(500, new ApiErrorResponse { Error = "INTERNAL_ERROR", Message = "An error occurred creating the work container" });
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, WorkContainerDto container)
    {
        if (id != container.Id)
            return BadRequest(new ApiErrorResponse { Error = "VALIDATION_ERROR", Message = "ID mismatch" });

        var existingProject = await _projectRepository.GetByIdAsync(id, includeRelations: true);
        if (existingProject == null)
            return NotFound();

        // Update properties
        var updatedProject = container.ToProject(existingProject);

        // Use Repository to update (service update might be limited to ViewModel)
        await _projectRepository.UpdateAsync(updatedProject);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!await _projectRepository.ExistsAsync(id))
            return NotFound();

        // Use Repository for Delete
        await _projectRepository.DeleteAsync(id);
        return NoContent();
    }
}
