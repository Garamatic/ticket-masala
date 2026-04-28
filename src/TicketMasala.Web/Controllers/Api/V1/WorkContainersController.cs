using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.Extensions;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.ViewModels.Api;
using TicketMasala.Web.ViewModels.Projects;

namespace TicketMasala.Web.Controllers.Api.V1;

/// <summary>
/// API for managing Work Containers (Projects) - Universal Entity Model canonical endpoints.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/work-containers")]
[ApiController]
public class WorkContainersController : ControllerBase
{
    private readonly IProjectWorkflowService _projectWorkflowService;
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WorkContainersController(
        IProjectWorkflowService projectWorkflowService,
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork)
    {
        _projectWorkflowService = projectWorkflowService;
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Get all work containers.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WorkContainerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var projects = await _projectRepository.GetAllAsync();
        return Ok(projects.Select(p => p.ToWorkContainerDto(p.Tasks?.Count ?? 0)));
    }

    /// <summary>
    /// Get a specific work container by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WorkContainerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var project = await _projectRepository.GetByIdAsync(id, includeRelations: true);
        if (project == null)
            return NotFound();

        return Ok(project.ToWorkContainerDto(project.Tasks?.Count ?? 0));
    }

    /// <summary>
    /// Create a new work container.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(WorkContainerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(WorkContainerDto container)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var userId = container.ManagerId ?? "SYSTEM";

        var vm = new NewProject
        {
            Name = container.Name,
            Description = container.Description ?? "",
            SelectedCustomerId = container.CustomerId,
            IsNewCustomer = false,
            ProjectType = container.ProjectType
        };

        var project = await _projectWorkflowService.CreateProjectAsync(vm, userId);

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
        if (container.CompletionTarget.HasValue && project.CompletionTarget != container.CompletionTarget)
        {
            project.CompletionTarget = container.CompletionTarget.Value;
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            await _projectRepository.UpdateAsync(project);
            await _unitOfWork.CommitAsync();
        }

        return CreatedAtAction(nameof(GetById), new { id = project.Guid, version = "1.0" }, project.ToWorkContainerDto());
    }

    /// <summary>
    /// Update an existing work container.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, WorkContainerDto container)
    {
        if (id != container.Id)
            throw new ArgumentException("ID mismatch between route and body");

        var existingProject = await _projectRepository.GetByIdAsync(id, includeRelations: true);
        if (existingProject == null)
            return NotFound();

        // Update properties
        var updatedProject = container.ToProject(existingProject);

        // Use Repository to update (service update might be limited to ViewModel)
        await _projectRepository.UpdateAsync(updatedProject);
        await _unitOfWork.CommitAsync();

        return NoContent();
    }

    /// <summary>
    /// Delete a work container.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!await _projectRepository.ExistsAsync(id))
            return NotFound();

        await _projectRepository.DeleteAsync(id);
        await _unitOfWork.CommitAsync();
        return NoContent();
    }
}
