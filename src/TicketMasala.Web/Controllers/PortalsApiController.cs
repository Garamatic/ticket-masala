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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainConfigurationService _domainConfig;
    private readonly ILogger<PortalsApiController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly ISystemClock _clock;
    private readonly IConfiguration _configuration;

    public PortalsApiController(
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork,
        IDomainConfigurationService domainConfig,
        ILogger<PortalsApiController> logger,
        IWebHostEnvironment environment,
        ISystemClock clock,
        IConfiguration configuration)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
        _domainConfig = domainConfig;
        _logger = logger;
        _environment = environment;
        _clock = clock;
        _configuration = configuration;
    }

    /// <summary>
    /// Validates the X-Portal-Secret header against the configured secret.
    /// Returns true if valid or if no secret is configured (dev mode).
    /// </summary>
    private bool ValidatePortalSecret()
    {
        var configuredSecret = _configuration["Portal:Secret"];
        if (string.IsNullOrEmpty(configuredSecret))
            return true; // No secret configured = dev mode, allow all

        if (!Request.Headers.TryGetValue("X-Portal-Secret", out var headerValue))
            return false;

        return headerValue == configuredSecret;
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
            await _unitOfWork.CommitAsync();

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
    /// Get tickets for a customer by email address.
    /// Requires X-Portal-Secret header to prevent unauthorized enumeration.
    /// </summary>
    [HttpGet("tickets")]
    public async Task<ActionResult<IEnumerable<PortalTicketDto>>> GetTicketsByEmail(
        [FromQuery] string email)
    {
        if (!ValidatePortalSecret())
        {
            _logger.LogWarning("Unauthorized portal ticket lookup attempt from {RemoteIp}", HttpContext.Connection.RemoteIpAddress);
            return Unauthorized(new { success = false, message = "Invalid or missing portal secret." });
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { success = false, message = "Email is required." });
        }

        try
        {
            // Find customer by email
            var customer = await _userRepository.GetUserByEmailAsync(email);
            if (customer == null)
            {
                // Return empty list for unknown customers — don’t leak whether email exists
                return Ok(new List<PortalTicketDto>());
            }

            // Get tickets for this customer
            var tickets = await _ticketRepository.GetByCustomerIdAsync(customer.Id);

            // Map to DTO
            var result = tickets.Select(t => new PortalTicketDto
            {
                Id = t.Guid.ToString(),
                Number = t.Guid.ToString().Substring(0, 8).ToUpper(),
                Type = t.WorkItemTypeCode ?? "DEMANDE",
                Title = t.Title ?? t.Description?.Substring(0, Math.Min(50, t.Description?.Length ?? 0)) + "..." ?? "Sans titre",
                Description = t.Description ?? string.Empty,
                Status = t.TicketStatus.ToString().ToLower(),
                Priority = ((int)t.PriorityScore).ToString(),
                Date = t.CreationDate.ToString("yyyy-MM-dd"),
                UpdatedAt = (t.CompletionDate ?? t.CreationDate).ToString("yyyy-MM-dd"),
                Tags = t.GerdaTags,
                HasAttachment = !string.IsNullOrEmpty(t.GerdaTags) && t.GerdaTags.Contains("Attachment:"),
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tickets for email {Email}", email);
            return StatusCode(500, new { success = false, message = "An error occurred while fetching tickets." });
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

/// <summary>
/// Ticket data returned by the portal API
/// </summary>
public class PortalTicketDto
{
    public string Id { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public string? Tags { get; set; }
    public bool HasAttachment { get; set; }
}
