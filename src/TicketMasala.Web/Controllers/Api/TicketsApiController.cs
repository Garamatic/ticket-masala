using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;
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

    /// <summary>Submit a ticket via the external intake API (anonymous, snake_case).</summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("ExternalSubmission")]
    [ProducesResponseType(typeof(ExternalTicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExternalTicketResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ExternalTicketResponse>> SubmitTicket(
        [FromBody] FlatTicketRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
            return BadRequest(new ExternalTicketResponse { Success = false, Message = "customer_email is required." });

        if (!IsValidEmail(request.CustomerEmail))
            return BadRequest(new ExternalTicketResponse { Success = false, Message = "customer_email must be a valid email address." });

        var description = SanitizeInput(request.Description ?? request.Subject ?? "No description provided.");
        if (description.Length > MaxExternalDescriptionLength)
            return BadRequest(new ExternalTicketResponse { Success = false, Message = $"description must be less than {MaxExternalDescriptionLength} characters." });

        var sanitizedName = SanitizeInput(request.CustomerName ?? "External User");
        if (sanitizedName.Length > MaxExternalNameLength)
            return BadRequest(new ExternalTicketResponse { Success = false, Message = $"customer_name must be less than {MaxExternalNameLength} characters." });

        var customer = await FindOrCreateCustomerAsync(request.CustomerEmail, sanitizedName);
        if (customer == null)
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ExternalTicketResponse { Success = false, Message = "Failed to create customer account." });

        var source = SanitizeInput(request.Source ?? "external-api");
        var body = $"{description}\n\n---\n*Submitted via: {source}*";

        var createResult = await _ticketLifecycle.ExecuteAsync(
            new CreateTicketCommand(body, customer.Id),
            new TicketContext("external"));

        if (!createResult.Success)
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ExternalTicketResponse { Success = false, Message = createResult.ErrorMessage ?? "Ticket creation failed." });

        var ticket = createResult.Ticket!;

        AppendExternalTag(ticket, source);

        await _ticketRepository.UpdateAsync(ticket);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation(
            "External ticket {TicketId} submitted via /api/tickets for {Email} (source: {Source})",
            ticket.Guid, customer.Email, source);

        return Ok(new ExternalTicketResponse
        {
            Success = true,
            TicketId = ticket.Guid.ToString(),
            ReferenceNumber = ticket.Guid.ToString()[..8].ToUpper(),
            Message = "Your request has been submitted successfully"
        });
    }

    /// <summary>Create a ticket from an external website.</summary>
    [HttpPost("external")]
    [AllowAnonymous]
    [EnableRateLimiting("ExternalSubmission")]
    [ProducesResponseType(typeof(ExternalTicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExternalTicketResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ExternalTicketResponse>> CreateExternalTicket(
        [FromBody] ExternalTicketRequest request)
    {
        // Validate input lengths
        if (string.IsNullOrWhiteSpace(request.Subject) || request.Subject.Length > MaxExternalSubjectLength)
        {
            return BadRequest(new ExternalTicketResponse { Success = false, Message = $"Subject is required and must be less than {MaxExternalSubjectLength} characters." });
        }

        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Length > MaxExternalDescriptionLength)
        {
            return BadRequest(new ExternalTicketResponse { Success = false, Message = $"Description is required and must be less than {MaxExternalDescriptionLength} characters." });
        }

        if (string.IsNullOrWhiteSpace(request.CustomerEmail) || !IsValidEmail(request.CustomerEmail))
        {
            return BadRequest(new ExternalTicketResponse { Success = false, Message = "Valid email address is required." });
        }

        if (!string.IsNullOrEmpty(request.CustomerName) && request.CustomerName.Length > MaxExternalNameLength)
        {
            return BadRequest(new ExternalTicketResponse { Success = false, Message = $"Name must be less than {MaxExternalNameLength} characters." });
        }

        // Sanitize inputs
        var sanitizedSubject = SanitizeInput(request.Subject);
        var sanitizedDescription = SanitizeInput(request.Description);
        var sanitizedCustomerName = SanitizeInput(request.CustomerName ?? "External User");
        var sanitizedSourceSite = SanitizeInput(request.SourceSite ?? "unknown");

        // Find or create customer by email
        var customer = await FindOrCreateCustomerAsync(request.CustomerEmail, sanitizedCustomerName);

        if (customer == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ExternalTicketResponse { Success = false, Message = "Failed to create customer account." });
        }

        var body = $"**{sanitizedSubject}**\n\n{sanitizedDescription}\n\n---\n*Submitted via: {sanitizedSourceSite}*";

        var createResult = await _ticketLifecycle.ExecuteAsync(
            new CreateTicketCommand(body, customer.Id),
            new TicketContext("external"));

        if (!createResult.Success)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ExternalTicketResponse { Success = false, Message = createResult.ErrorMessage ?? "Failed to create external ticket" });
        }

        var ticket = createResult.Ticket!;

        // Append external source tag
        AppendExternalTag(ticket, sanitizedSourceSite);

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

    /// <summary>Get all tickets.</summary>
    [HttpGet]
    [Authorize]
    [Obsolete("Use /api/v1/work-items endpoints instead")]
    [ProducesResponseType(typeof(IEnumerable<TicketViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TicketViewModel>>> GetAll()
    {
        var tickets = await _ticketReadService.GetAllTicketsAsync();
        return Ok(tickets);
    }

    /// <summary>Get a specific ticket by ID.</summary>
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

    /// <summary>Create a new WorkItem.</summary>
    [HttpPost("create")]
    [Authorize]
    [ProducesResponseType(typeof(WorkItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkItemResponse>> CreateWorkItem(
        [FromBody] CreateWorkItemRequest request)
    {
        _logger.LogInformation("Creating WorkItem with title: {Title}, domain: {Domain}",
            request.Title, request.DomainId);

        if (string.IsNullOrEmpty(request.CustomerId))
        {
            return BadRequest(new { error = "CustomerId is required." });
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
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = createResult.ErrorMessage ?? "Failed to create work item" });
        }

        var ticket = createResult.Ticket!;

        var modifiedBy = request.CustomerId ?? "system";
        ticket.SetDomain(request.DomainId);
        ticket.UpdateTitle(request.Title, modifiedBy);
        ticket.UpdateCustomFields(customFieldsJson, modifiedBy);
        await _ticketRepository.UpdateAsync(ticket);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation("Created WorkItem {Id} successfully", ticket.Guid);

        return CreatedAtAction(
            nameof(GetById),
            new { id = ticket.Guid },
            MapToWorkItemResponse(ticket));
    }

    /// <summary>Resolve a ticket and optionally attach a billable amount.</summary>
    [HttpPost("{id:guid}/resolve")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveTicket(Guid id, [FromBody] ResolveTicketRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ResolutionNotes))
        {
            return BadRequest(new { error = "Resolution notes are required." });
        }

        if (request.BillableAmount is < 0)
        {
            return BadRequest(new { error = "Billable amount cannot be negative." });
        }

        var userId = _userManager.GetUserId(User) ?? "system";

        var resolveResult = await _ticketLifecycle.ExecuteAsync(
            new ResolveTicketCommand(id, request.ResolutionNotes, request.BillableAmount),
            new TicketContext(userId));

        if (!resolveResult.Success)
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

    /// <summary>Find existing customer or create a new one.</summary>
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

    /// <summary>Generates a cryptographically secure random password.</summary>
    private static string GenerateSecurePassword()
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";
        const string all = upper + lower + digits + special;
        const int length = 20;

        var password = new char[length];

        // Guarantee at least one character from each required class
        password[0] = upper[System.Security.Cryptography.RandomNumberGenerator.GetInt32(upper.Length)];
        password[1] = lower[System.Security.Cryptography.RandomNumberGenerator.GetInt32(lower.Length)];
        password[2] = digits[System.Security.Cryptography.RandomNumberGenerator.GetInt32(digits.Length)];
        password[3] = special[System.Security.Cryptography.RandomNumberGenerator.GetInt32(special.Length)];

        // Fill remaining positions from the full pool
        for (int i = 4; i < length; i++)
            password[i] = all[System.Security.Cryptography.RandomNumberGenerator.GetInt32(all.Length)];

        // Fisher-Yates shuffle so mandatory chars land at random positions
        for (int i = length - 1; i > 0; i--)
        {
            int j = System.Security.Cryptography.RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }


    /// <summary>Validates email format.</summary>
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, emailPattern, RegexOptions.IgnoreCase);
    }

    private static readonly string[] DangerousPatterns = new[]
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

    /// <summary>Sanitizes user input.</summary>
    private static string SanitizeInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        foreach (var pattern in DangerousPatterns)
        {
            input = input.Replace(pattern, "", StringComparison.OrdinalIgnoreCase);
        }

        return input.Trim();
    }

    /// <summary>Appends an external source tag to a ticket.</summary>
    private static void AppendExternalTag(Ticket ticket, string source)
    {
        ticket.GerdaTags = string.IsNullOrEmpty(ticket.GerdaTags)
            ? $"External-Request,{source}"
            : $"{ticket.GerdaTags},External-Request,{source}";
    }

    /// <summary>Maps Ticket entity to WorkItemResponse DTO.</summary>
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

/// <summary>
/// Flat snake_case ticket intake payload consumed by POST /api/tickets.
/// Matches the event shape generated by partner portals and integration tests.
/// </summary>
public class FlatTicketRequest
{
    [JsonPropertyName("customer_email")]
    public string? CustomerEmail { get; set; }

    [JsonPropertyName("customer_name")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Alternative to description for backwards compat.</summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    [JsonPropertyName("tenant_id")]
    public string? TenantId { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("ticket_id")]
    public string? TicketId { get; set; }
}

