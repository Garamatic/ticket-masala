using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Observers;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.Security;
using TicketMasala.Web.Data;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Web.Engine.Compiler;
using TicketMasala.Web.Engine.GERDA.Configuration;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

public interface ITicketWorkflowService
{
    Task<Ticket> CreateTicketAsync(string description, string customerId, string? responsibleId, Guid? projectGuid, DateTime? completionTarget);
    Task<bool> UpdateTicketAsync(Ticket ticket);
    Task<bool> AssignTicketAsync(Guid ticketGuid, string agentId);
    Task<bool> AssignTicketWithProjectAsync(Guid ticketGuid, string? agentId, Guid? projectGuid);
    Task<TicketComment> AddCommentAsync(Guid ticketId, string body, bool isInternal, string authorId);
    Task<bool> RequestReviewAsync(Guid ticketId, string requesterId);
    Task<bool> SubmitReviewAsync(Guid ticketId, int score, string feedback, bool approved, string reviewerId);
    Task<TimeLog> LogTimeAsync(Guid ticketId, string userId, double hours, DateTime date, string description);

    // Internal notification helpers exposed if needed, or kept private if only used internally
    // Task NotifyObserversAssignedAsync(Ticket ticket, Employee assignee);
    // Task NotifyObserversUpdatedAsync(Ticket ticket);
    // Task NotifyObserversCommentedAsync(TicketComment comment);
}

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
        Domain.TicketDispatchService ticketDispatchService)
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
            Status = responsible != null ? "Assigned" : "New",
            Title = description.Length > 50 ? description.Substring(0, 47) + "..." : description,
            DomainId = defaultDomainId,
            ConfigVersionId = currentConfigVersion,
            TicketStatus = responsible != null ? Status.Assigned : Status.Pending,
            CompletionTarget = completionTarget ?? DateTime.UtcNow.AddDays(14),
            CreatorGuid = Guid.Parse(customer.Id),
            CreationDate = DateTime.UtcNow,
            Comments = new List<TicketComment>()
        };

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
        await _auditService.LogActionAsync(ticket.Guid, "Created", GetCurrentUserId());

        return ticket;
    }

    public async Task<bool> UpdateTicketAsync(Ticket ticket)
    {
        try
        {
            // PII Scrubbing
            ticket.Description = _piiScrubber.Scrub(ticket.Description);

            // Validate Transition Rules
            var entry = _context.Entry(ticket);
            if (entry.State == EntityState.Modified)
            {
                var originalStatus = (Status)entry.Property(t => t.TicketStatus).OriginalValue;
                if (originalStatus != ticket.TicketStatus)
                {
                    var user = _httpContextAccessor.HttpContext?.User;
                    if (user != null)
                    {
                        // Create a temporary ticket with original status to check transition FROM that status
                        var currentStatus = ticket.TicketStatus; // Logic: new status
                        ticket.TicketStatus = originalStatus; // Set back to old status for check
                        var canTransition = _ruleEngine.CanTransition(ticket, currentStatus, user);
                        ticket.TicketStatus = currentStatus; // Restore new status
                        if (!canTransition)
                        {
                            throw new DomainRuleException($"Transition from {originalStatus} to {ticket.TicketStatus} is not allowed by domain rules.");
                        }
                    }
                }
            }

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
            CreatedAt = DateTime.UtcNow,
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
        if (ticket == null) return false;

        ticket.ReviewStatus = ReviewStatus.Pending;
        await _ticketRepository.UpdateAsync(ticket);

        await _auditService.LogActionAsync(ticketId, "ReviewRequested", requesterId);

        return true;
    }

    public async Task<bool> SubmitReviewAsync(Guid ticketId, int score, string feedback, bool approved, string reviewerId)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, includeRelations: false);
        if (ticket == null) return false;

        ticket.ReviewStatus = approved ? ReviewStatus.Approved : ReviewStatus.Rejected;

        var review = new QualityReview
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            ReviewerId = reviewerId,
            Score = score,
            Comments = feedback,
            CreatedAt = DateTime.UtcNow,
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
