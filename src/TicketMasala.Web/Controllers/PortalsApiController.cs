using Microsoft.AspNetCore.Mvc;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Data;
using TicketMasala.Web.ViewModels.Portal;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Configuration;
using TicketMasala.Web.Engine.GERDA.Configuration;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Controllers;

/// <summary>
/// API controller for handling public portal submissions from customer-facing portals.
/// Supports anonymous ticket creation for demo purposes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PortalsApiController : ControllerBase
{
    private readonly MasalaDbContext _context;
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IDomainConfigurationService _domainConfig;
    private readonly ILogger<PortalsApiController> _logger;
    private readonly IWebHostEnvironment _environment;

    public PortalsApiController(
        MasalaDbContext context,
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        IDomainConfigurationService domainConfig,
        ILogger<PortalsApiController> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _domainConfig = domainConfig;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Submit a new ticket from a customer portal.
    /// Supports file uploads, geolocation, and custom fields.
    /// </summary>
    [HttpPost("submit")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PortalSubmissionResponse>> Submit(
        [FromForm] PortalSubmissionViewModel model)
    {
        try
        {
            _logger.LogInformation("Portal submission received: {Description}", model.Description);

            // Find or create customer
            ApplicationUser? customer = null;
            if (!string.IsNullOrEmpty(model.CustomerEmail))
            {
                customer = await _userRepository.GetUserByEmailAsync(model.CustomerEmail);
                
                // Create customer if doesn't exist (for demo purposes)
                if (customer == null)
                {
                    customer = new ApplicationUser
                    {
                        UserName = model.CustomerEmail,
                        Email = model.CustomerEmail,
                        FirstName = model.CustomerName?.Split(' ').FirstOrDefault() ?? "Portal",
                        LastName = model.CustomerName?.Split(' ').Skip(1).FirstOrDefault() ?? "User",
                        PhoneNumber = model.CustomerPhone,
                        EmailConfirmed = true
                    };

                    var result = await _userRepository.CreateCustomerAsync(customer, "Portal@123");
                    if (!result)
                    {
                        return BadRequest(new PortalSubmissionResponse
                        {
                            Success = false,
                            Message = "Failed to create customer account"
                        });
                    }
                }
            }

            // Create the ticket
            var ticket = new Ticket
            {
                Description = model.Description,
                Status = TicketStatus.New,
                Type = TicketType.Task,
                CustomerId = customer?.Id,
                PriorityScore = model.PriorityScore ?? 5,
                GerdaTags = model.Tags,
                CompletionTargetDate = DateTime.UtcNow.AddDays(7) // Default SLA
            };

            // Handle geolocation
            if (model.Latitude.HasValue && model.Longitude.HasValue)
            {
                ticket.GerdaTags = string.IsNullOrEmpty(ticket.GerdaTags)
                    ? $"Geo:{model.Latitude},{model.Longitude}"
                    : $"{ticket.GerdaTags},Geo:{model.Latitude},{model.Longitude}";
            }

            // Handle file upload
            if (model.Attachment != null && model.Attachment.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "portal");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{model.Attachment.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Attachment.CopyToAsync(fileStream);
                }

                // Store file reference in tags for now
                ticket.GerdaTags = string.IsNullOrEmpty(ticket.GerdaTags)
                    ? $"Attachment:{uniqueFileName}"
                    : $"{ticket.GerdaTags},Attachment:{uniqueFileName}";
            }

            // Save to database
            await _context.Tickets.AddAsync(ticket);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Portal ticket created: {TicketGuid}", ticket.Guid);

            return Ok(new PortalSubmissionResponse
            {
                Success = true,
                Message = "Your request has been submitted successfully",
                TicketGuid = ticket.Guid,
                TicketNumber = $"#{ticket.Id}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing portal submission");
            return StatusCode(500, new PortalSubmissionResponse
            {
                Success = false,
                Message = "An error occurred while processing your request. Please try again."
            });
        }
    }

    /// <summary>
    /// Health check endpoint for portals
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
