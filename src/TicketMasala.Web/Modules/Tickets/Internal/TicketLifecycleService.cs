using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Repositories;
using TicketMasala.Domain.Services;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Modules.Tickets.Internal;

internal interface ITicketLifecycleService
{
    Task<Ticket> CreateAsync(CreateTicketCommand command, CancellationToken ct = default);
    Task UpdateAsync(Ticket ticket, UpdateTicketCommand command, CancellationToken ct = default);
    Task AssignAsync(Ticket ticket, AssignTicketCommand command, CancellationToken ct = default);
    Task TransitionStatusAsync(Ticket ticket, TransitionStatusCommand command, CancellationToken ct = default);
}

internal class TicketLifecycleService : ITicketLifecycleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly ITicketAssignmentService _assignmentService;
    private readonly IDomainConfigurationService _domainConfig;

    public TicketLifecycleService(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        ITicketAssignmentService assignmentService,
        IDomainConfigurationService domainConfig)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _assignmentService = assignmentService;
        _domainConfig = domainConfig;
    }

    public async Task<Ticket> CreateAsync(CreateTicketCommand command, CancellationToken ct = default)
    {
        // Validate command before processing
        command.Validate();

        var customer = await _userRepository.GetCustomerByIdAsync(command.CustomerId).ConfigureAwait(false);
        if (customer == null)
            throw new InvalidOperationException($"Customer {command.CustomerId} not found");

        var domainId = command.DomainId ?? _domainConfig.GetDefaultDomainId();

        var ticket = Ticket.CreateFromPortal(
            command.Description,
            command.CustomerId,
            completionTarget: command.CompletionTarget);

        ticket.DomainId = domainId;
        ticket.WorkItemTypeCode = command.WorkItemTypeCode;
        ticket.ProjectGuid = command.ProjectGuid;

        // Parse custom fields into JSON
        if (command.CustomFields.Count > 0)
        {
            ticket.UpdateCustomFields(
                System.Text.Json.JsonSerializer.Serialize(command.CustomFields),
                command.CreatedByUserId);
        }

        if (!string.IsNullOrEmpty(command.ResponsibleId))
        {
            var employee = await _userRepository.GetEmployeeByIdAsync(command.ResponsibleId).ConfigureAwait(false);
            if (employee != null)
            {
                ticket.SetResponsible(employee);
                ticket.TicketStatus = Domain.Common.Status.Assigned;
                ticket.SyncStatus();
            }
        }

        await _unitOfWork.Tickets.AddAsync(ticket).ConfigureAwait(false);
        await _unitOfWork.CommitAsync(ct).ConfigureAwait(false);
        return ticket;
    }

    public async Task UpdateAsync(Ticket ticket, UpdateTicketCommand command, CancellationToken ct = default)
    {
        // IMPORTANT: The 'ticket' parameter should be a freshly loaded entity from the database.
        // Callers MUST load the ticket within the same unit of work to ensure optimistic concurrency works.
        // See TicketModule.UpdateAsync for the correct pattern.

        // Check status transition first (optimistic concurrency check)
        // This must happen before any modifications to prevent partial updates on conflict
        var actualStatusString = ticket.TicketStatus.ToString();
        if (actualStatusString != command.TicketStatus)
        {
            // Status differs from what UI expected - parse and verify valid transition
            var newStatus = command.ParseStatusOrThrow();

            if (!Ticket.IsValidTransition(ticket.TicketStatus, newStatus))
            {
                throw new InvalidOperationException(
                    $"Cannot transition from {actualStatusString} to {newStatus}. " +
                    $"The ticket may have been modified by another user. Please refresh and try again.");
            }

            ticket.TransitionTo(newStatus, command.ModifiedByUserId);
        }

        // Apply other updates only after concurrency check passes
        ticket.UpdateDescription(command.Description, command.ModifiedByUserId);

        ticket.CompletionTarget = command.CompletionTarget;
        ticket.CustomerId = command.CustomerId;
        ticket.ProjectGuid = command.ProjectGuid;

        if (command.CustomFields.Count > 0)
        {
            ticket.UpdateCustomFields(
                System.Text.Json.JsonSerializer.Serialize(command.CustomFields),
                command.ModifiedByUserId);
        }

        // Note: We do NOT call _unitOfWork.Tickets.UpdateAsync(ticket) here because
        // the ticket is already being tracked by EF Core (it was loaded fresh in this UoW).
        // EF Core will automatically detect changes. Calling UpdateAsync would mark all
        // properties as modified, potentially causing unnecessary UPDATE statements.
        await _unitOfWork.CommitAsync(ct).ConfigureAwait(false);
    }

    public async Task AssignAsync(Ticket ticket, AssignTicketCommand command, CancellationToken ct = default)
    {
        var employee = await _userRepository.GetEmployeeByIdAsync(command.ResponsibleId).ConfigureAwait(false);
        if (employee == null)
            throw new InvalidOperationException($"Employee {command.ResponsibleId} not found");

        await _assignmentService.AssignToEmployeeAsync(
            ticket,
            employee,
            command.AssignedByUserId,
            command.AssignedByRoles).ConfigureAwait(false);

        await _unitOfWork.Tickets.UpdateAsync(ticket).ConfigureAwait(false);
        await _unitOfWork.CommitAsync(ct).ConfigureAwait(false);
    }

    public async Task TransitionStatusAsync(Ticket ticket, TransitionStatusCommand command, CancellationToken ct = default)
    {
        // Optimistic concurrency check: verify ticket hasn't changed since UI loaded
        if (ticket.TicketStatus.ToString() != command.FromStatus)
        {
            throw new InvalidOperationException(
                $"Ticket status has changed from {command.FromStatus} to {ticket.TicketStatus}. " +
                "Please refresh and try again.");
        }

        if (!Enum.TryParse<Domain.Common.Status>(command.ToStatus, out var targetStatus))
            throw new InvalidOperationException($"Invalid status: {command.ToStatus}");

        ticket.TransitionTo(targetStatus, command.ChangedByUserId);
        await _unitOfWork.Tickets.UpdateAsync(ticket).ConfigureAwait(false);
        await _unitOfWork.CommitAsync(ct).ConfigureAwait(false);
    }
}
