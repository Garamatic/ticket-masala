using Microsoft.AspNetCore.Http;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets.Domain;
using TicketMasala.Web.Observers;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// Service responsible for ticket assignment operations.
/// Coordinates assignment to agents and project association.
/// </summary>
/// <remarks>
/// Note: This is an application service that coordinates between the domain
/// TicketDispatchService and observers/auditing. Domain assignment logic
/// should be in the domain layer.
/// </remarks>
public interface ITicketAssignmentFacade
{
    /// <summary>
    /// Assigns a ticket to an agent using the dispatch service.
    /// </summary>
    /// <param name="ticketGuid">The ticket to assign</param>
    /// <param name="agentId">The agent to assign to</param>
    /// <param name="assignedByUserId">User performing the assignment (for audit)</param>
    /// <returns>True if assignment succeeded</returns>
    Task<bool> AssignAsync(Guid ticketGuid, string agentId, string? assignedByUserId);

    /// <summary>
    /// Assigns a ticket to an agent and optionally associates it with a project.
    /// </summary>
    /// <param name="ticketGuid">The ticket to assign</param>
    /// <param name="agentId">Optional agent to assign</param>
    /// <param name="projectGuid">Optional project to associate</param>
    /// <param name="assignedByUserId">User performing the assignment (for audit)</param>
    /// <returns>True if assignment succeeded</returns>
    Task<bool> AssignWithProjectAsync(Guid ticketGuid, string? agentId, Guid? projectGuid, string? assignedByUserId);
}

/// <summary>
/// Implementation of ticket assignment workflow.
/// </summary>
internal class TicketAssignmentFacade : ITicketAssignmentFacade
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly TicketDispatchService _dispatchService;
    private readonly IEnumerable<ITicketObserver> _observers;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TicketAssignmentFacade> _logger;

    public TicketAssignmentFacade(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        TicketDispatchService dispatchService,
        IEnumerable<ITicketObserver> observers,
        IAuditService auditService,
        INotificationService notificationService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TicketAssignmentFacade> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _dispatchService = dispatchService ?? throw new ArgumentNullException(nameof(dispatchService));
        _observers = observers ?? throw new ArgumentNullException(nameof(observers));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> AssignAsync(Guid ticketGuid, string agentId, string? assignedByUserId)
    {
        // Delegate to the domain dispatch service for core assignment logic
        // Note: assignedByUserId is not passed here because the dispatch service
        // accesses HttpContext directly for the audit user ID
        var success = await _dispatchService.AssignTicketAsync(
            ticketGuid,
            agentId,
            _userRepository,
            _observers,
            _notificationService,
            _auditService,
            _httpContextAccessor);

        return success;
    }

    public async Task<bool> AssignWithProjectAsync(Guid ticketGuid, string? agentId, Guid? projectGuid, string? assignedByUserId)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketGuid, includeRelations: false);

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
            ticket.SyncStatus();
        }

        if (projectGuid.HasValue)
        {
            // Verify project exists
            var project = await _unitOfWork.Projects.GetByIdAsync(projectGuid.Value, includeRelations: false);
            if (project == null)
            {
                _logger.LogWarning("Project {ProjectGuid} not found for assignment", projectGuid.Value);
                return false;
            }

            ticket.ProjectGuid = projectGuid.Value;
        }

        // Queue ticket update (not yet committed)
        await _unitOfWork.Tickets.UpdateAsync(ticket);

        // Audit trail (also queued)
        await _auditService.LogActionAsync(ticketGuid, "Assigned", assignedByUserId);

        // Commit all changes in a single transaction
        await _unitOfWork.CommitAsync();

        _logger.LogInformation(
            "Ticket {TicketGuid} assigned to agent {AgentId} and project {ProjectGuid}",
            ticketGuid,
            agentId ?? "(none)",
            projectGuid ?? Guid.Empty);

        // Notify observers (after commit to ensure data is persisted)
        if (agent != null)
        {
            await NotifyAssignedAsync(ticket, agent);
        }
        else
        {
            await NotifyUpdatedAsync(ticket);
        }

        return true;
    }

    private async Task NotifyAssignedAsync(Ticket ticket, Employee assignee)
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

    private async Task NotifyUpdatedAsync(Ticket ticket)
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
}
