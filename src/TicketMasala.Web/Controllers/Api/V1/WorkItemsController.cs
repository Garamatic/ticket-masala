using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketMasala.Web.Engine.GERDA.Tickets;
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
    private readonly ITicketWorkflowService _ticketWorkflowService;
    private readonly ITicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJsonParsingService _jsonParsingService;

    public WorkItemsController(
        ITicketWorkflowService ticketWorkflowService,
        ITicketRepository ticketRepository,
        IUnitOfWork unitOfWork,
        IJsonParsingService jsonParsingService)
    {
        _ticketWorkflowService = ticketWorkflowService;
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

        // Use Service for Create to ensure business rules/observers run
        var ticket = await _ticketWorkflowService.CreateTicketAsync(
            workItem.Description,
            customerId,
            workItem.AssignedHandlerId,
            workItem.ContainerId,
            workItem.CompletionTarget
        );

        // Post-creation update for fields not in Service.Create signature
        // Use domain methods to ensure proper validation and event raising
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

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

        // Update properties
        var updatedTicket = workItem.ToTicket(existingTicket);

        // Use Service to persist to ensure Rules/Observers run
        var result = await _ticketWorkflowService.UpdateTicketAsync(updatedTicket);

        if (!result)
            throw new InvalidOperationException("Failed to update work item");

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
        var success = await _ticketWorkflowService.ResolveTicketAsync(
            id,
            request.ResolutionNotes,
            request.BillableAmount,
            resolvedByUserId
        );

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
