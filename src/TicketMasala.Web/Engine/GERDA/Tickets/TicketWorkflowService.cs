using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Enums;
using TicketMasala.Domain.Services;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.Compiler;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Web.Engine.Security;
using TicketMasala.Web.Observers;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// NOTE: This service is being replaced by the TicketModule deep module.
/// New code should use ITicketModule instead for ticket lifecycle operations.
/// </summary>
public class TicketWorkflowService : ITicketWorkflowService
{
    private readonly MasalaDbContext _context;
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IEnumerable<ITicketObserver> _observers;
    private readonly IEnumerable<ICommentObserver> _commentObservers;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRuleEngineService _ruleEngine;
    private readonly IDomainConfigurationService _domainConfig;
    private readonly IPiiScrubberService _piiScrubber;
    private readonly Domain.TicketNotificationService _ticketNotificationService;
    private readonly ILogger<TicketWorkflowService> _logger;
    private readonly Domain.TicketDispatchService _ticketDispatchService;
    private readonly ISystemClock _clock;

    public TicketWorkflowService(
        MasalaDbContext context,
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        IEnumerable<ITicketObserver> observers,
        IEnumerable<ICommentObserver> commentObservers,
        INotificationService notificationService,
        IAuditService auditService,
        IHttpContextAccessor httpContextAccessor,
        IRuleEngineService ruleEngine,
        IDomainConfigurationService domainConfig,
        IPiiScrubberService piiScrubber,
        Domain.TicketNotificationService ticketNotificationService,
        ILogger<TicketWorkflowService> logger,
        Domain.TicketDispatchService ticketDispatchService,
        ISystemClock clock)
    {
        _context = context;
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _observers = observers;
        _commentObservers = commentObservers;
        _notificationService = notificationService;
        _auditService = auditService;
        _httpContextAccessor = httpContextAccessor;
        _ruleEngine = ruleEngine;
        _domainConfig = domainConfig;
        _piiScrubber = piiScrubber;
        _ticketNotificationService = ticketNotificationService;
        _logger = logger;
        _ticketDispatchService = ticketDispatchService;
        _clock = clock;
    }

    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? null;
    }

    public async Task<Ticket> CreateTicketAsync(
        string description,
        string customerId,
        string? responsibleId,
        Guid? projectGuid,
        DateTime? completionTarget)
    {
        // PII Scrubbing
        description = _piiScrubber.Scrub(description);

        var customer = await _userRepository.GetCustomerByIdAsync(customerId);
        if (customer == null)
        {
            throw new ArgumentException("Customer not found", nameof(customerId));
        }

        Employee? responsible = null;
        if (!string.IsNullOrWhiteSpace(responsibleId))
        {
            responsible = await _userRepository.GetEmployeeByIdAsync(responsibleId);
        }

        var currentConfigVersion = _domainConfig.GetCurrentConfigVersionId();
        var defaultDomainId = _domainConfig.GetDefaultDomainId();

        var ticket = new Ticket
        {
            Description = description,
            Customer = customer,
            CustomerId = customerId,
            Responsible = responsible,
            Title = description.Length > 50 ? description.Substring(0, 47) + "..." : description,
            DomainId = defaultDomainId,
            ConfigVersionId = currentConfigVersion,
            TicketStatus = responsible != null ? Status.Assigned : Status.Pending,
            CompletionTarget = completionTarget ?? _clock.UtcNow.AddDays(14),
            CreatorGuid = Guid.Parse(customer.Id),
        };
        ticket.SyncStatus();

        // Add ticket via repository
        await _ticketRepository.AddAsync(ticket);

        // If a project is selected, add the ticket to that project
        if (projectGuid.HasValue && projectGuid.Value != Guid.Empty)
        {
            var project = await _projectRepository.GetByIdAsync(projectGuid.Value, includeRelations: true);

            if (project != null)
            {
                project.Tasks.Add(ticket);
                await _projectRepository.UpdateAsync(project);
            }
        }

        _logger.LogInformation("Ticket {TicketGuid} created successfully", ticket.Guid);

        // Notify observers
        await NotifyObserversUpdatedAsync(ticket);

        // Audit Log
        await _auditService.LogActionAsync(ticket.Guid, ActivityType.Created.GetDisplayText(), GetCurrentUserId());

        return ticket;
    }

    public async Task<bool> UpdateTicketAsync(Ticket ticket)
    {
        try
        {
            // PII Scrubbing
            ticket.Description = _piiScrubber.Scrub(ticket.Description);

            // Note: Authorization and transition validation is now done in the orchestrator
            // using domain methods (ticket.ValidateCanEdit, ticket.ValidateCanChangeStatus)
            // before calling this service method.

            await _ticketRepository.UpdateAsync(ticket);

            // Notify observers
            await NotifyObserversUpdatedAsync(ticket);

            // Delegate notification logic
            await _ticketNotificationService.NotifyStatusChangeAsync(ticket);

            // Audit Log
            await _auditService.LogActionAsync(ticket.Guid, "Updated", GetCurrentUserId());

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update ticket {TicketGuid}", ticket.Guid);
            return false;
        }
    }

    public async Task<bool> AssignTicketAsync(Guid ticketGuid, string agentId)
    {
        return await _ticketDispatchService.AssignTicketAsync(
            ticketGuid,
            agentId,
            _userRepository,
            _observers,
            _notificationService,
            _auditService,
            _httpContextAccessor
        );
    }

    public async Task<bool> AssignTicketWithProjectAsync(Guid ticketGuid, string? agentId, Guid? projectGuid)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketGuid, includeRelations: false);

        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketGuid} not found for assignment", ticketGuid);
            return false;
        }

        Employee? agent = null;

        if (!string.IsNullOrEmpty(agentId))
        {
            agent = await _userRepository.GetEmployeeByIdAsync(agentId);

            if (agent == null)
            {
                _logger.LogWarning("Agent {AgentId} not found", agentId);
                return false;
            }

            ticket.ResponsibleId = agentId;
            ticket.TicketStatus = Status.Assigned;
        }

        if (projectGuid.HasValue)
        {
            ticket.ProjectGuid = projectGuid.Value;
        }

        await _ticketRepository.UpdateAsync(ticket);

        _logger.LogInformation("Ticket {TicketGuid} assigned to agent {AgentId} and project {ProjectGuid}",
            ticketGuid, agentId, projectGuid);

        if (agent != null)
        {
            await NotifyObserversAssignedAsync(ticket, agent);
        }
        else
        {
            await NotifyObserversUpdatedAsync(ticket);
        }

        return true;
    }

    public async Task<TicketComment> AddCommentAsync(Guid ticketId, string body, bool isInternal, string authorId)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, includeRelations: true);
        if (ticket == null)
        {
            throw new ArgumentException("Ticket not found", nameof(ticketId));
        }

        var comment = new TicketComment
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Body = body,
            IsInternal = isInternal,
            CreatedAt = _clock.UtcNow,
            AuthorId = authorId,
            Ticket = ticket
        };

        _context.TicketComments.Add(comment);
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(ticketId, "Commented", authorId, "Comment", null, isInternal ? "Internal Note" : "Public Reply");

        await NotifyObserversCommentedAsync(comment);

        return comment;
    }

    public async Task<bool> RequestReviewAsync(Guid ticketId, string requesterId)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, includeRelations: false);
        if (ticket == null)
            return false;

        ticket.ReviewStatus = ReviewStatus.Pending;
        await _ticketRepository.UpdateAsync(ticket);

        await _auditService.LogActionAsync(ticketId, "ReviewRequested", requesterId);

        return true;
    }

    public async Task<bool> SubmitReviewAsync(Guid ticketId, int score, string feedback, bool approved, string reviewerId)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, includeRelations: false);
        if (ticket == null)
            return false;

        ticket.ReviewStatus = approved ? ReviewStatus.Approved : ReviewStatus.Rejected;

        var review = new QualityReview
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            ReviewerId = reviewerId,
            Score = score,
            Comments = feedback,
            CreatedAt = _clock.UtcNow,
            IsApproved = approved
        };

        _context.QualityReviews.Add(review);
        await _ticketRepository.UpdateAsync(ticket);

        await _auditService.LogActionAsync(ticketId, approved ? "ReviewApproved" : "ReviewRejected", reviewerId, "QualityReview", null, feedback);

        return true;
    }

    public async Task<TimeLog> LogTimeAsync(Guid ticketId, string userId, double hours, DateTime date, string description)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, includeRelations: false);
        if (ticket == null)
        {
            throw new ArgumentException("Ticket not found", nameof(ticketId));
        }

        var timeLog = new TimeLog
        {
            TicketId = ticketId,
            UserId = userId,
            Hours = hours,
            Date = date,
            Description = description
        };

        _context.TimeLogs.Add(timeLog);
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(ticketId, "TimeLogged", userId, "TimeLog", null, $"{hours} hours");

        _logger.LogInformation("Time logged for ticket {TicketId}: {Hours} hours by {UserId}", ticketId, hours, userId);

        return timeLog;
    }

    public async Task<Ticket> ResolveTicketAsync(
        Guid ticketGuid,
        string resolutionNotes,
        decimal? billableAmount,
        string resolvedByUserId)
    {
        // Load ticket with customer relation for event data
        var ticket = await _ticketRepository.GetByIdAsync(ticketGuid, includeRelations: true);
        if (ticket == null)
        {
            throw new ArgumentException("Ticket not found", nameof(ticketGuid));
        }

        // Resolve the ticket (this transitions status and raises domain event)
        ticket.Resolve(resolutionNotes, billableAmount, resolvedByUserId);

        // Create outbox message for reliable event publishing
        var customer = ticket.Customer;
        var eventPayload = new TicketResolvedEventMessage
        {
            EventType = "ticket.resolved",
            Timestamp = DateTime.UtcNow.ToString("O"),
            Source = "ticket-masala",
            TicketId = ticket.Guid.ToString(),
            CustomerEmail = customer?.Email ?? "unknown@example.com",
            CustomerName = customer?.Name ?? "Unknown Customer",
            ServiceDescription = ticket.Title,
            Amount = billableAmount ?? 0,
            TenantId = ticket.DomainId,
            ResolvedAt = DateTime.UtcNow.ToString("O"),
            ResolutionNotes = resolutionNotes
        };

        var outboxMessage = new TicketMasala.Domain.Entities.OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "ticket.resolved",
            Payload = System.Text.Json.JsonSerializer.Serialize(eventPayload, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            }),
            RoutingKey = "event.ticket.resolved",
            CreatedAt = DateTime.UtcNow
        };

        _context.OutboxMessages.Add(outboxMessage);

        // Save ticket and outbox message in the same transaction
        await _ticketRepository.UpdateAsync(ticket);
        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogActionAsync(ticketGuid, "Resolved", resolvedByUserId, "Ticket", null,
            $"Billable: {billableAmount?.ToString("C") ?? "N/A"}, Notes: {resolutionNotes.Substring(0, Math.Min(100, resolutionNotes.Length))}...");

        _logger.LogInformation(
            "Ticket {TicketGuid} resolved by {UserId} with amount {Amount:C}",
            ticketGuid,
            resolvedByUserId,
            billableAmount);

        // Notify observers
        await NotifyObserversUpdatedAsync(ticket);

        return ticket;
    }

    /// <summary>
    /// Message format for ticket.resolved event (matches IC-001 schema).
    /// Uses snake_case property names for JSON serialization.
    /// </summary>
    private class TicketResolvedEventMessage
    {
        public string EventType { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string TicketId { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ServiceDescription { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public string ResolvedAt { get; set; } = string.Empty;
        public string? ResolutionNotes { get; set; }
    }

    private async Task NotifyObserversAssignedAsync(Ticket ticket, Employee assignee)
    {
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnTicketAssignedAsync(ticket, assignee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Observer {ObserverType} failed on ticket assignment", observer.GetType().Name);
            }
        }
    }

    private async Task NotifyObserversUpdatedAsync(Ticket ticket)
    {
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnTicketUpdatedAsync(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Observer {ObserverType} failed on ticket update", observer.GetType().Name);
            }
        }
    }

    private async Task NotifyObserversCommentedAsync(TicketComment comment)
    {
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnTicketCommentedAsync(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Observer {ObserverType} failed on ticket comment", observer.GetType().Name);
            }
        }

        foreach (var observer in _commentObservers)
        {
            try
            {
                await observer.OnCommentAddedAsync(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CommentObserver {ObserverType} failed on comment added", observer.GetType().Name);
            }
        }
    }
}
