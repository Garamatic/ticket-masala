using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Domain.Common; // Added
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Configuration;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.ViewModels.Portal;

namespace TicketMasala.Web.Controllers;

/// <summary>
/// API controller for handling public portal submissions from customer-facing portals.
/// Supports anonymous ticket creation for demo purposes.
/// </summary>
[ApiController]
[Route("api/portal")]
[AllowAnonymous]
public class PortalsApiController : ControllerBase
{

    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IDomainConfigurationService _domainConfig;
    private readonly ILogger<PortalsApiController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly ISystemClock _clock;

    public PortalsApiController(

        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        IDomainConfigurationService domainConfig,
        ILogger<PortalsApiController> logger,
        IWebHostEnvironment environment,
        ISystemClock clock)
    {

        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _domainConfig = domainConfig;
        _logger = logger;
        _environment = environment;
        _clock = clock;
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

            // Create the ticket using factory method
            var ticket = Ticket.CreateFromPortal(
                model.Description,
                customer?.Id,
                priorityScore: model.PriorityScore ?? 5,
                tags: model.Tags,
                completionTarget: _clock.UtcNow.AddDays(7));

            // Handle geolocation
            if (model.Latitude.HasValue && model.Longitude.HasValue)
            {
                ticket.AddGerdaTag($"Geo:{model.Latitude},{model.Longitude}");
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

                // Store file reference in tags
                ticket.AddGerdaTag($"Attachment:{uniqueFileName}");
            }

            // Save to database
            await _ticketRepository.AddAsync(ticket);


            _logger.LogInformation("Portal ticket created: {TicketGuid}", ticket.Guid);

            return Ok(new PortalSubmissionResponse
            {
                Success = true,
                Message = "Your request has been submitted successfully",
                TicketGuid = ticket.Guid,
                TicketNumber = $"#{ticket.Guid}" // Using Guid instead of non-existent Id
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
        return Ok(new { status = "healthy", timestamp = _clock.UtcNow });
    }
}
