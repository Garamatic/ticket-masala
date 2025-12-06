using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TicketMasala.Web.ViewModels.Tickets;
using TicketMasala.Web.Models;
using TicketMasala.Web.Services.Core;
using TicketMasala.Web.Services.Tickets;
using TicketMasala.Web.Services.Projects;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Services.Background;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Controllers.Api;

/// <summary>
/// REST API for Ticket management - includes external submission endpoint
/// </summary>
[ApiController]
[Route("api/v1/tickets")]
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
    private async Task<Customer?> FindOrCreateCustomerAsync(string email, string name)
    {
        // Try to find existing customer
        var existingUser = await _userRepository.GetUserByEmailAsync(email);
        
        if (existingUser is Customer existingCustomer)
        {
            return existingCustomer;
        }

        // Create new customer
        var nameParts = name.Split(' ', 2);
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[1] : "";

        var newCustomer = new Customer
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
    public async Task<ActionResult<TicketDetailsViewModel>> GetById(Guid id)
    {
        var ticket = await _ticketService.GetTicketDetailsAsync(id);
        
        if (ticket == null)
        {
            return NotFound();
        }

        return Ok(ticket);
    }

}
