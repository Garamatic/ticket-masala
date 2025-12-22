using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.ViewModels.Api;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Extensions;
using System.Security.Claims;

namespace TicketMasala.Web.Controllers.Api.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/work-items")]
[ApiController]
[Authorize]
public class WorkItemsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<WorkItemsController> _logger;

    public WorkItemsController(
        ITicketService ticketService,
        ITicketRepository ticketRepository,
        ILogger<WorkItemsController> logger)
    {
        _ticketService = ticketService;
        _ticketRepository = ticketRepository;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WorkItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            // Use Repository to get full entities for proper DTO mapping
            // (Service only returns limited ViewModels)
            var tickets = await _ticketRepository.GetAllAsync();
            return Ok(tickets.Select(t => t.ToWorkItemDto()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving work items");
            return StatusCode(500, new ApiErrorResponse { Error = "INTERNAL_ERROR", Message = ex.ToString() }); // Expose stack for debugging
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WorkItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var ticket = await _ticketRepository.GetByIdAsync(id); // Use Repo for consistency
            if (ticket == null)
                return NotFound();

            return Ok(ticket.ToWorkItemDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving work item {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Error = "INTERNAL_ERROR", Message = ex.ToString() });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(WorkItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(WorkItemDto workItem)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Resolve CustomerId
            string? customerId = workItem.CustomerId;
            if (string.IsNullOrEmpty(customerId))
            {
                // Try to get from authenticated user
                customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            }

            if (string.IsNullOrEmpty(customerId))
            {
                return BadRequest(new ApiErrorResponse { Error = "VALIDATION_ERROR", Message = "CustomerId is required and could not be determined from context." });
            }

            // Use Service for Create to ensure business rules/observers run
            var ticket = await _ticketService.CreateTicketAsync(
                workItem.Description,
                customerId,
                workItem.AssignedHandlerId,
                workItem.ContainerId,
                workItem.CompletionTarget
            );

            // Post-creation update for fields not in Service.Create signature
            bool needsUpdate = false;
            if (!string.IsNullOrEmpty(workItem.Title) && workItem.Title != "New Ticket")
            {
                ticket.Title = workItem.Title;
                needsUpdate = true;
            }
            if (!string.IsNullOrEmpty(workItem.DomainId) && workItem.DomainId != "IT")
            {
                ticket.DomainId = workItem.DomainId;
                needsUpdate = true;
            }
            if (!string.IsNullOrEmpty(workItem.TypeCode))
            {
                ticket.WorkItemTypeCode = workItem.TypeCode;
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                await _ticketService.UpdateTicketAsync(ticket);
            }

            return CreatedAtAction(nameof(GetById), new { id = ticket.Guid, version = "1.0" }, ticket.ToWorkItemDto());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiErrorResponse { Error = "VALIDATION_ERROR", Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating work item");
            return StatusCode(500, new ApiErrorResponse { Error = "INTERNAL_ERROR", Message = "An error occurred creating the work item" });
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, WorkItemDto workItem)
    {
        if (id != workItem.Id)
            return BadRequest(new ApiErrorResponse { Error = "VALIDATION_ERROR", Message = "ID mismatch" });

        var existingTicket = await _ticketRepository.GetByIdAsync(id);
        if (existingTicket == null)
            return NotFound();

        // Update properties
        var updatedTicket = workItem.ToTicket(existingTicket);

        // Use Service to persist to ensure Rules/Observers run
        var result = await _ticketService.UpdateTicketAsync(updatedTicket);

        if (!result)
            return StatusCode(500, new ApiErrorResponse { Error = "INTERNAL_ERROR", Message = "Failed to update work item" });

        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!await _ticketRepository.ExistsAsync(id))
            return NotFound();

        // Use Repository for Delete as Service doesn't expose it
        await _ticketRepository.DeleteAsync(id);
        return NoContent();
    }
}
