using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Repositories;
using TicketMasala.Domain.Services;
using TicketMasala.Web.Engine.GERDA.Configuration;

namespace TicketMasala.Web.Modules.Tickets.Internal;

internal interface ITicketLifecycleService
{
    // Note: These methods don't accept CancellationToken because the underlying
    // repositories (ITicketRepository, IUserRepository) don't support it.
    Task<Ticket> CreateAsync(CreateTicketCommand command);
    Task UpdateAsync(Ticket ticket, UpdateTicketCommand command);
    Task AssignAsync(Ticket ticket, AssignTicketCommand command);
    Task TransitionStatusAsync(Ticket ticket, TransitionStatusCommand command);
}

internal class TicketLifecycleService : ITicketLifecycleService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITicketAssignmentService _assignmentService;
    private readonly IDomainConfigurationService _domainConfig;

    public TicketLifecycleService(
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        ITicketAssignmentService assignmentService,
        IDomainConfigurationService domainConfig)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _assignmentService = assignmentService;
        _domainConfig = domainConfig;
    }

    public async Task<Ticket> CreateAsync(CreateTicketCommand command)
    {
        var customer = await _userRepository.GetCustomerByIdAsync(command.CustomerId);
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
            var employee = await _userRepository.GetEmployeeByIdAsync(command.ResponsibleId);
            if (employee != null)
            {
                ticket.SetResponsible(employee);
                ticket.TicketStatus = Domain.Common.Status.Assigned;
                ticket.SyncStatus();
            }
        }

        await _ticketRepository.AddAsync(ticket);
        return ticket;
    }

    public async Task UpdateAsync(Ticket ticket, UpdateTicketCommand command)
    {
        // Check status transition first (optimistic concurrency check)
        // This must happen before any modifications to prevent partial updates on conflict
        var actualStatus = ticket.TicketStatus.ToString();
        if (actualStatus != command.TicketStatus)
        {
            // Status differs from what UI expected - verify it's a valid intentional transition
            if (!Enum.TryParse<Domain.Common.Status>(command.TicketStatus, out var newStatus))
                throw new InvalidOperationException($"Invalid status: {command.TicketStatus}");

            if (!Ticket.IsValidTransition(ticket.TicketStatus, newStatus))
            {
                throw new InvalidOperationException(
                    $"Cannot transition from {actualStatus} to {newStatus}. " +
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

        await _ticketRepository.UpdateAsync(ticket);
    }

    public async Task AssignAsync(Ticket ticket, AssignTicketCommand command)
    {
        var employee = await _userRepository.GetEmployeeByIdAsync(command.ResponsibleId);
        if (employee == null)
            throw new InvalidOperationException($"Employee {command.ResponsibleId} not found");

        await _assignmentService.AssignToEmployeeAsync(
            ticket,
            employee,
            command.AssignedByUserId,
            command.AssignedByRoles);

        await _ticketRepository.UpdateAsync(ticket);
    }

    public async Task TransitionStatusAsync(Ticket ticket, TransitionStatusCommand command)
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
        await _ticketRepository.UpdateAsync(ticket);
    }
}
