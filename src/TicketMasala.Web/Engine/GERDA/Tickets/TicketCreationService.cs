using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Enums;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Web.Engine.Security;
using TicketMasala.Web.Observers;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// Service responsible for creating new tickets.
/// Handles PII scrubbing, customer lookup, assignment, project association,
/// and initial notifications.
/// </summary>
[Obsolete("Use ITicketLifecycle and command records instead. This interface will be removed in a future release.", false)]
public interface ITicketCreationService
{
    /// <summary>
    /// Creates a new ticket with the specified parameters.
    /// </summary>
    /// <param name="description">Ticket description (will be PII-scrubbed)</param>
    /// <param name="customerId">Customer creating the ticket</param>
    /// <param name="responsibleId">Optional employee to assign</param>
    /// <param name="projectGuid">Optional project to associate</param>
    /// <param name="completionTarget">Optional target completion date</param>
    /// <param name="creatorUserId">User performing the creation (for audit)</param>
    /// <returns>The created ticket</returns>
    /// <exception cref="ArgumentException">Thrown if customer not found</exception>
    Task<Ticket> CreateAsync(
        string description,
        string customerId,
        string? responsibleId,
        Guid? projectGuid,
        DateTime? completionTarget,
        string? creatorUserId);
}

/// <summary>
/// Implementation of ticket creation workflow.
/// </summary>
internal class TicketCreationService : ITicketCreationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IDomainConfigurationService _domainConfig;
    private readonly IPiiScrubberService _piiScrubber;
    private readonly IEnumerable<ITicketObserver> _observers;
    private readonly IAuditService _auditService;
    private readonly ISystemClock _clock;
    private readonly ILogger<TicketCreationService> _logger;

    public TicketCreationService(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        IDomainConfigurationService domainConfig,
        IPiiScrubberService piiScrubber,
        IEnumerable<ITicketObserver> observers,
        IAuditService auditService,
        ISystemClock clock,
        ILogger<TicketCreationService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _domainConfig = domainConfig ?? throw new ArgumentNullException(nameof(domainConfig));
        _piiScrubber = piiScrubber ?? throw new ArgumentNullException(nameof(piiScrubber));
        _observers = observers ?? throw new ArgumentNullException(nameof(observers));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Ticket> CreateAsync(
        string description,
        string customerId,
        string? responsibleId,
        Guid? projectGuid,
        DateTime? completionTarget,
        string? creatorUserId)
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

        // Auto-generate title from description
        var title = description.Length > 50
            ? description[..47] + "..."
            : description;

        var ticket = new Ticket
        {
            Description = description,
            Customer = customer,
            CustomerId = customerId,
            Responsible = responsible,
            Title = title,
            DomainId = defaultDomainId,
            ConfigVersionId = currentConfigVersion,
            TicketStatus = responsible != null ? Status.Assigned : Status.Pending,
            CompletionTarget = completionTarget ?? _clock.UtcNow.AddDays(14),
            CreatorGuid = Guid.TryParse(customer.Id, out var creatorGuid) ? creatorGuid : null,
        };
        ticket.SyncStatus();

        // Queue ticket add (not yet committed)
        await _unitOfWork.Tickets.AddAsync(ticket);

        // If a project is selected, add the ticket to that project (also queued)
        if (projectGuid.HasValue && projectGuid.Value != Guid.Empty)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectGuid.Value, includeRelations: true);

            if (project != null)
            {
                project.Tasks.Add(ticket);
                await _unitOfWork.Projects.UpdateAsync(project);
            }
        }

        // Audit trail (also queued)
        await _auditService.LogActionAsync(
            ticket.Guid,
            ActivityType.Created.GetDisplayText(),
            creatorUserId);

        // Commit all changes in a single transaction
        await _unitOfWork.CommitAsync();

        _logger.LogInformation("Ticket {TicketGuid} created successfully", ticket.Guid);

        // Notify observers (after commit to ensure data is persisted)
        await NotifyObserversAsync(ticket, responsible);

        return ticket;
    }

    private async Task NotifyObserversAsync(Ticket ticket, Employee? assignee)
    {
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnTicketUpdatedAsync(ticket);

                if (assignee != null)
                {
                    await observer.OnTicketAssignedAsync(ticket, assignee);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Observer {ObserverType} failed during ticket creation",
                    observer.GetType().Name);
            }
        }
    }
}
