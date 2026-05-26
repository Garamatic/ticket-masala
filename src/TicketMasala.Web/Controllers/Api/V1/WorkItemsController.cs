using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;
using TicketMasala.Web.Extensions;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Services;
using TicketMasala.Web.ViewModels.Api;

namespace TicketMasala.Web.Controllers.Api.V1;

/// <summary>
/// API for managing Work Items (Tickets) - Universal Entity Model canonical endpoints.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/work-items")]
[ApiController]
[Authorize]
public class WorkItemsController : ControllerBase
{
    private readonly ITicketLifecycle _ticketLifecycle;
    private readonly ITicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJsonParsingService _jsonParsingService;

    public WorkItemsController(
        ITicketLifecycle ticketLifecycle,
        ITicketRepository ticketRepository,
        IUnitOfWork unitOfWork,
        IJsonParsingService jsonParsingService)
    {
        _ticketLifecycle = ticketLifecycle;
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _jsonParsingService = jsonParsingService;
    }

    /// <summary>
    /// Get all work items.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WorkItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var tickets = await _ticketRepository.GetAllAsync();
        return Ok(tickets.Select(t => t.ToWorkItemDto(_jsonParsingService)));
    }

    /// <summary>
    /// Get a specific work item by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WorkItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var ticket = await _ticketRepository.GetByIdAsync(id);
        if (ticket == null)
            return NotFound();

        return Ok(ticket.ToWorkItemDto(_jsonParsingService));
    }

    /// <summary>
    /// Create a new work item.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(WorkItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(WorkItemDto workItem)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        // Resolve CustomerId
        string? customerId = workItem.CustomerId;
        if (string.IsNullOrEmpty(customerId))
        {
            customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        if (string.IsNullOrEmpty(customerId))
        {
            throw new ArgumentException("CustomerId is required and could not be determined from context.");
        }

        // Resolve current user for audit context
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        // Use Service for Create to ensure business rules/observers run
        var result = await _ticketLifecycle.ExecuteAsync(
            new CreateTicketCommand(
                workItem.Description,
                customerId,
                workItem.AssignedHandlerId,
                workItem.ContainerId,
                workItem.CompletionTarget),
            new TicketContext(currentUserId));

        if (!result.Success)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "Failed to create work item");
        }

        var ticket = result.Ticket!;

        // Post-creation update for fields not in Service.Create signature
        // Use domain methods to ensure proper validation and event raising

        if (!string.IsNullOrEmpty(workItem.Title) && workItem.Title != "New Ticket")
        {
            ticket.UpdateTitle(workItem.Title, currentUserId);
        }
        if (!string.IsNullOrEmpty(workItem.DomainId) && workItem.DomainId != "IT")
        {
            ticket.SetDomain(workItem.DomainId);
        }
        if (!string.IsNullOrEmpty(workItem.TypeCode))
        {
            ticket.SetWorkItemTypeCode(workItem.TypeCode);
        }

        // Always update to capture any domain method changes
        await _ticketRepository.UpdateAsync(ticket);
        await _unitOfWork.CommitAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = ticket.Guid, version = "1.0" },
            ticket.ToWorkItemDto(_jsonParsingService));
    }

    /// <summary>
    /// Update an existing work item.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, WorkItemDto workItem)
    {
        if (id != workItem.Id)
            throw new ArgumentException("ID mismatch between route and body");

        var existingTicket = await _ticketRepository.GetByIdAsync(id);
        if (existingTicket == null)
            return NotFound();

        // Update properties on the tracked entity - DO NOT call _ticketRepository.UpdateAsync here
        // The TicketWorkflowService will handle persistence with proper business rules/observers
        workItem.ToTicket(existingTicket);

        // Persist changes directly (TODO: migrate to UpdateTicketCommand for full observer/audit support)
        await _ticketRepository.UpdateAsync(existingTicket);
        await _unitOfWork.CommitAsync();

        return NoContent();
    }

    /// <summary>
    /// Delete a work item.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!await _ticketRepository.ExistsAsync(id))
            return NotFound();

        await _ticketRepository.DeleteAsync(id);
        await _unitOfWork.CommitAsync();
        return NoContent();
    }

    /// <summary>
    /// Resolve a work item (mark as completed with resolution notes and billable amount).
    /// </summary>
    [HttpPost("{id}/resolve")]
    [ProducesResponseType(typeof(WorkItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveWorkItemRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        // Get current user ID
        var resolvedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(resolvedByUserId))
        {
            return Unauthorized();
        }

        // Resolve the ticket via workflow service
        var resolveResult = await _ticketLifecycle.ExecuteAsync(
            new ResolveTicketCommand(id, request.ResolutionNotes, request.BillableAmount),
            new TicketContext(resolvedByUserId));

        var success = resolveResult.Success;

        if (!success)
        {
            return NotFound();
        }

        var ticket = await _ticketRepository.GetByIdAsync(id, includeRelations: true);
        if (ticket == null)
        {
            return NotFound();
        }

        return Ok(ticket.ToWorkItemDto(_jsonParsingService));
    }
}
