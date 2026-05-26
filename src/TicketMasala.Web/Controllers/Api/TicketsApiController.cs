using System.Text.Json;
using System.Text.RegularExpressions;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.ViewModels.Api;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Controllers.Api;

/// <summary>
/// REST API for WorkItem (Ticket) management - includes external submission endpoint.
/// Routes: /api/v{version}/tickets (legacy) and /api/v{version}/workitems (UEM canonical)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tickets")]
[Route("api/v{version:apiVersion}/workitems")]
[Route("api/tickets")]
[Produces("application/json")]
public class TicketsApiController : ControllerBase
{
    private readonly ITicketLifecycle _ticketLifecycle;
    private readonly ITicketReadService _ticketReadService;
    private readonly IUserRepository _userRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TicketsApiController> _logger;
    private readonly ISystemClock _clock;

    // Security limits for external submissions
    private const int MaxExternalSubjectLength = 200;
    private const int MaxExternalDescriptionLength = 5000;
    private const int MaxExternalNameLength = 100;

    public TicketsApiController(
        ITicketLifecycle ticketLifecycle,
        ITicketReadService ticketReadService,
        IUserRepository userRepository,
        ITicketRepository ticketRepository,
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        ILogger<TicketsApiController> logger,
        ISystemClock clock)
    {
        _ticketLifecycle = ticketLifecycle;
        _ticketReadService = ticketReadService;
        _userRepository = userRepository;
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _logger = logger;
        _clock = clock;
    }

    /// <summary>
    /// Create a ticket from an external website (e.g., partner company site).
    /// Rate limited to prevent abuse.
    /// </summary>
    /// <param name="request">External ticket request data</param>
    /// <returns>Ticket ID and reference number</returns>
    [HttpPost("external")]
    [AllowAnonymous]
    [EnableRateLimiting("ExternalSubmission")]
    [ProducesResponseType(typeof(ExternalTicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExternalTicketResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ExternalTicketResponse>> CreateExternalTicket(
        [FromBody] ExternalTicketRequest request)
    {
        // Validate input lengths (anti-spam)
        if (string.IsNullOrWhiteSpace(request.Subject) || request.Subject.Length > MaxExternalSubjectLength)
        {
            throw new ArgumentException($"Subject is required and must be less than {MaxExternalSubjectLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Length > MaxExternalDescriptionLength)
        {
            throw new ArgumentException($"Description is required and must be less than {MaxExternalDescriptionLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerEmail) || !IsValidEmail(request.CustomerEmail))
        {
            throw new ArgumentException("Valid email address is required.");
        }

        if (!string.IsNullOrEmpty(request.CustomerName) && request.CustomerName.Length > MaxExternalNameLength)
        {
            throw new ArgumentException($"Name must be less than {MaxExternalNameLength} characters.");
        }

        // Sanitize inputs to prevent injection
        var sanitizedSubject = SanitizeInput(request.Subject);
        var sanitizedDescription = SanitizeInput(request.Description);
        var sanitizedCustomerName = SanitizeInput(request.CustomerName ?? "External User");
        var sanitizedSourceSite = SanitizeInput(request.SourceSite ?? "unknown");

        // Find or create customer by email
        var customer = await FindOrCreateCustomerAsync(request.CustomerEmail, sanitizedCustomerName);

        if (customer == null)
        {
            throw new InvalidOperationException("Failed to create customer account.");
        }

        // Create the ticket
        var description = $"**{sanitizedSubject}**\n\n{sanitizedDescription}\n\n---\n*Submitted via: {sanitizedSourceSite}*";

        var createResult = await _ticketLifecycle.ExecuteAsync(
            new CreateTicketCommand(description, customer.Id),
            new TicketContext("external"));

        if (!createResult.Success)
        {
            throw new InvalidOperationException(createResult.ErrorMessage ?? "Failed to create external ticket");
        }

        var ticket = createResult.Ticket!;

        // Add external source tag
        ticket.GerdaTags = string.IsNullOrEmpty(ticket.GerdaTags)
            ? $"External-Request,{sanitizedSourceSite}"
            : $"{ticket.GerdaTags},External-Request,{sanitizedSourceSite}";

        await _ticketRepository.UpdateAsync(ticket);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation(
            "External ticket {TicketId} created successfully for customer {CustomerId} from {Source}",
            ticket.Guid,
            customer.Id,
            sanitizedSourceSite);

        return Ok(new ExternalTicketResponse
        {
            Success = true,
            TicketId = ticket.Guid.ToString(),
            ReferenceNumber = ticket.Guid.ToString()[..8].ToUpper(),
            Message = "Your request has been submitted successfully"
        });
    }

    /// <summary>
    /// Get all tickets (authenticated users only).
    /// </summary>
    [HttpGet]
    [Authorize]
    [Obsolete("Use /api/v1/work-items endpoints instead")]
    [ProducesResponseType(typeof(IEnumerable<TicketViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TicketViewModel>>> GetAll()
    {
        var tickets = await _ticketReadService.GetAllTicketsAsync();
        return Ok(tickets);
    }

    /// <summary>
    /// Get a specific ticket by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [Obsolete("Use /api/v1/work-items endpoints instead")]
    [ProducesResponseType(typeof(TicketDetailsViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDetailsViewModel>> GetById(Guid id)
    {
        var ticket = await _ticketReadService.GetTicketDetailsAsync(id);

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
    [ProducesResponseType(typeof(WorkItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkItemResponse>> CreateWorkItem(
        [FromBody] CreateWorkItemRequest request)
    {
        _logger.LogInformation("Creating WorkItem with title: {Title}, domain: {Domain}",
            request.Title, request.DomainId);

        // Validate required fields
        if (string.IsNullOrEmpty(request.CustomerId))
        {
            throw new ArgumentException("CustomerId is required.");
        }

        // Map custom fields to JSON
        var customFieldsJson = request.CustomFields != null
            ? JsonSerializer.Serialize(request.CustomFields)
            : "{}";

        // Create the ticket using internal service
        var createResult = await _ticketLifecycle.ExecuteAsync(
            new CreateTicketCommand(
                $"**{request.Title}**\n\n{request.Description}",
                request.CustomerId,
                request.AssigneeId,
                request.WorkContainerId,
                request.CompletionTarget ?? _clock.UtcNow.AddDays(14)),
            new TicketContext(_userManager.GetUserId(User) ?? "system"));

        if (!createResult.Success)
        {
            throw new InvalidOperationException(createResult.ErrorMessage ?? "Failed to create work item");
        }

        var ticket = createResult.Ticket!;

        // Update domain-specific fields
        ticket.SetDomain(request.DomainId);
        ticket.UpdateTitle(request.Title, request.CustomerId ?? "system");
        ticket.UpdateCustomFields(customFieldsJson, request.CustomerId ?? "system");
        await _ticketRepository.UpdateAsync(ticket);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation("Created WorkItem {Id} successfully", ticket.Guid);

        return CreatedAtAction(
            nameof(GetById),
            new { id = ticket.Guid },
            MapToWorkItemResponse(ticket));
    }

    /// <summary>
    /// Resolve a ticket and optionally attach a billable amount.
    /// Emits a ticket.resolved event consumed by odoo-integration.
    /// </summary>
    [HttpPost("{id:guid}/resolve")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveTicket(Guid id, [FromBody] ResolveTicketRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ResolutionNotes))
        {
            throw new ArgumentException("Resolution notes are required.");
        }

        if (request.BillableAmount is < 0)
        {
            throw new ArgumentException("Billable amount cannot be negative.");
        }

        var userId = _userManager.GetUserId(User) ?? "system";

        var resolveResult = await _ticketLifecycle.ExecuteAsync(
            new ResolveTicketCommand(id, request.ResolutionNotes, request.BillableAmount),
            new TicketContext(userId));

        var success = resolveResult.Success;

        if (!success)
        {
            return NotFound(new { error = "Ticket not found or could not be resolved." });
        }

        return Ok(new
        {
            ticket_id = id.ToString(),
            status = "resolved",
            resolution_notes = request.ResolutionNotes,
            billable_amount = request.BillableAmount
        });
    }

    /// <summary>
    /// Find existing customer or create a new one.
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
        var nameParts = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[1] : "";

        var newCustomer = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Phone = "",
            EmailConfirmed = true
        };

        // Generate a secure random password
        var randomPassword = GenerateSecurePassword();
        var result = await _userManager.CreateAsync(newCustomer, randomPassword);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(newCustomer, "Customer");
            _logger.LogInformation("Created new customer {Email} from external submission", email);
            return newCustomer;
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        _logger.LogWarning("Failed to create customer {Email}: {Errors}", email, errors);
        return null;
    }

    /// <summary>
    /// Generates a cryptographically secure random password.
    /// </summary>
    private static string GenerateSecurePassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        using var random = System.Security.Cryptography.RandomNumberGenerator.Create();
        var password = new char[20];
        var byteBuffer = new byte[sizeof(int)];

        for (int i = 0; i < password.Length; i++)
        {
            random.GetBytes(byteBuffer);
            var randomInt = BitConverter.ToInt32(byteBuffer, 0) & int.MaxValue;
            password[i] = chars[randomInt % chars.Length];
        }

        return new string(password);
    }

    /// <summary>
    /// Validates email format.
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, emailPattern, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Sanitizes user input to prevent injection attacks.
    /// </summary>
    private static string SanitizeInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        // Remove potentially dangerous patterns
        var dangerousPatterns = new[]
        {
            "<script",
            "</script",
            "javascript:",
            "onerror=",
            "onload=",
            "onclick=",
            "onmouseover=",
            "eval(",
            "expression("
        };

        var sanitized = input;
        foreach (var pattern in dangerousPatterns)
        {
            sanitized = sanitized.Replace(pattern, "", StringComparison.OrdinalIgnoreCase);
        }

        return sanitized.Trim();
    }

    /// <summary>
    /// Maps internal Ticket entity to WorkItemResponse DTO.
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

public class ResolveTicketRequest
{
    public string ResolutionNotes { get; set; } = string.Empty;
    public decimal? BillableAmount { get; set; }
}
