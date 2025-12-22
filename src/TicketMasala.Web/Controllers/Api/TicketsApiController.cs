using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Web.ViewModels.Api;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Engine.Ingestion.Background;
using TicketMasala.Web.Repositories;
using System.Text.Json;

namespace TicketMasala.Web.Controllers.Api;

/// <summary>
/// REST API for WorkItem (Ticket) management - includes external submission endpoint.
/// Routes: /api/v1/tickets (legacy) and /api/v1/workitems (UEM canonical)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tickets")]
[Route("api/v{version:apiVersion}/workitems")]
[Produces("application/json")]
public class TicketsApiController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly ITicketFactory _ticketFactory;
    private readonly IUserRepository _userRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TicketsApiController> _logger;

    public TicketsApiController(
        ITicketService ticketService,
        ITicketFactory ticketFactory,
        IUserRepository userRepository,
        ITicketRepository ticketRepository,
        UserManager<ApplicationUser> userManager,
        ILogger<TicketsApiController> logger)
    {
        _ticketService = ticketService;
        _ticketFactory = ticketFactory;
        _userRepository = userRepository;
        _ticketRepository = ticketRepository;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Create a ticket from an external website (e.g., partner company site)
    /// This endpoint allows anonymous access for demo purposes
    /// </summary>
    /// <param name="request">External ticket request data</param>
    /// <returns>Ticket ID and reference number</returns>
    [HttpPost("external")]
    [AllowAnonymous]
    public async Task<ActionResult<ExternalTicketResponse>> CreateExternalTicket([FromBody] ExternalTicketRequest request)
    {
        try
        {
            _logger.LogInformation("External ticket submission from {Source} for {Email}",
                request.SourceSite, request.CustomerEmail);

            // 1. Find or create customer by email
            var customer = await FindOrCreateCustomerAsync(request.CustomerEmail, request.CustomerName);

            if (customer == null)
            {
                return BadRequest(new ExternalTicketResponse
                {
                    Success = false,
                    Message = "Failed to create customer account"
                });
            }

            // 2. Create the ticket
            var description = $"**{request.Subject}**\n\n{request.Description}";

            if (!string.IsNullOrEmpty(request.SourceSite))
            {
                description += $"\n\n---\n*Submitted via: {request.SourceSite}*";
            }

            var ticket = await _ticketService.CreateTicketAsync(
                description: description,
                customerId: customer.Id,
                responsibleId: null, // GERDA will assign
                projectGuid: null,
                completionTarget: DateTime.UtcNow.AddDays(14)
            );

            // 3. Add external source tag
            ticket.GerdaTags = string.IsNullOrEmpty(ticket.GerdaTags)
                ? $"External-Request,{request.SourceSite ?? "unknown"}"
                : $"{ticket.GerdaTags},External-Request,{request.SourceSite ?? "unknown"}";

            await _ticketRepository.UpdateAsync(ticket);

            _logger.LogInformation("External ticket {TicketId} created successfully", ticket.Guid);

            return Ok(new ExternalTicketResponse
            {
                Success = true,
                TicketId = ticket.Guid.ToString(),
                ReferenceNumber = ticket.Guid.ToString().Substring(0, 8).ToUpper(),
                Message = "Your request has been submitted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating external ticket");
            return StatusCode(500, new ExternalTicketResponse
            {
                Success = false,
                Message = "An error occurred processing your request"
            });
        }
    }

    /// <summary>
    /// Find existing customer or create a new one
    /// </summary>
    private async Task<ApplicationUser?> FindOrCreateCustomerAsync(string email, string name)
    {
        // Try to find existing customer
        var existingUser = await _userRepository.GetUserByEmailAsync(email);

        if (existingUser != null)
        {
            return existingUser;
        }

        // Create new customer
        var nameParts = name.Split(' ', 2);
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[1] : "";

        var newCustomer = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Phone = "", // Required property
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(newCustomer, "ExternalUser123!"); // Default password

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(newCustomer, "Customer");
            _logger.LogInformation("Created new customer {Email} from external submission", email);
            return newCustomer;
        }

        _logger.LogWarning("Failed to create customer {Email}: {Errors}",
            email, string.Join(", ", result.Errors.Select(e => e.Description)));
        return null;
    }

    /// <summary>
    /// Get all tickets (authenticated users only)
    /// </summary>
    [HttpGet]
    [Authorize]
    [Obsolete("Use /api/v1/work-items endpoints instead")]
    public async Task<ActionResult<IEnumerable<TicketViewModel>>> GetAll()
    {
        var tickets = await _ticketService.GetAllTicketsAsync();
        return Ok(tickets);
    }

    /// <summary>
    /// Get a specific ticket by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [Obsolete("Use /api/v1/work-items endpoints instead")]
    public async Task<ActionResult<TicketDetailsViewModel>> GetById(Guid id)
    {
        var ticket = await _ticketService.GetTicketDetailsAsync(id);

        if (ticket == null)
        {
            return NotFound();
        }

        return Ok(ticket);
    }

    /// <summary>
    /// Create a new WorkItem (Universal Entity Model terminology).
    /// Valid DomainId values are sourced from masala_domains.yaml configuration.
    /// </summary>
    /// <param name="request">WorkItem creation request</param>
    /// <returns>Created WorkItem response</returns>
    [HttpPost("create")]
    [Authorize]
    public async Task<ActionResult<WorkItemResponse>> CreateWorkItem([FromBody] CreateWorkItemRequest request)
    {
        try
        {
            _logger.LogInformation("Creating WorkItem with title: {Title}, domain: {Domain}",
                request.Title, request.DomainId);

            // Validate required fields
            if (string.IsNullOrEmpty(request.CustomerId))
            {
                return BadRequest(new { error = "CustomerId is required" });
            }

            // Map custom fields to JSON
            var customFieldsJson = request.CustomFields != null
                ? JsonSerializer.Serialize(request.CustomFields)
                : "{}";

            // Create the ticket using internal service
            var ticket = await _ticketService.CreateTicketAsync(
                description: $"**{request.Title}**\n\n{request.Description}",
                customerId: request.CustomerId,
                responsibleId: request.AssigneeId,
                projectGuid: request.WorkContainerId,
                completionTarget: request.CompletionTarget ?? DateTime.UtcNow.AddDays(14)
            );

            // Update domain-specific fields
            ticket.DomainId = request.DomainId;
            ticket.Title = request.Title;
            ticket.CustomFieldsJson = customFieldsJson;
            await _ticketRepository.UpdateAsync(ticket);

            _logger.LogInformation("Created WorkItem {Id} successfully", ticket.Guid);

            return CreatedAtAction(
                nameof(GetById),
                new { id = ticket.Guid },
                MapToWorkItemResponse(ticket));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating WorkItem");
            return StatusCode(500, new { error = "An error occurred creating the work item" });
        }
    }

    /// <summary>
    /// Maps internal Ticket entity to WorkItemResponse DTO
    /// </summary>
    private static WorkItemResponse MapToWorkItemResponse(Ticket ticket)
    {
        return new WorkItemResponse
        {
            Id = ticket.Guid,
            Title = ticket.Title,
            Description = ticket.Description,
            DomainId = ticket.DomainId,
            Status = ticket.Status,
            CreatedAt = ticket.CreationDate,
            CompletionTarget = ticket.CompletionTarget,
            CompletedAt = ticket.CompletionDate,
            EstimatedEffortPoints = ticket.EstimatedEffortPoints,
            PriorityScore = ticket.PriorityScore,
            RecommendedAssignee = ticket.RecommendedProjectName,
            CustomerName = ticket.Customer?.FullName,
            AssigneeName = ticket.Responsible?.FullName,
            WorkContainerId = ticket.ProjectGuid,
            WorkContainerName = ticket.Project?.Name
        };
    }

}
