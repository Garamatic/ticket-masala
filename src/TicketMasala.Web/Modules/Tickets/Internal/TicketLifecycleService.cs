using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Repositories;
using TicketMasala.Domain.Services;
using TicketMasala.Web.Engine.GERDA.Configuration;

namespace TicketMasala.Web.Modules.Tickets.Internal;

internal interface ITicketLifecycleService
{
    Task<Ticket> CreateAsync(CreateTicketCommand command, CancellationToken ct);
    Task UpdateAsync(Ticket ticket, UpdateTicketCommand command, CancellationToken ct);
    Task AssignAsync(Ticket ticket, AssignTicketCommand command, CancellationToken ct);
    Task TransitionStatusAsync(Ticket ticket, TransitionStatusCommand command, CancellationToken ct);
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

    public async Task<Ticket> CreateAsync(CreateTicketCommand command, CancellationToken ct)
    {
        var customer = await _userRepository.GetCustomerByIdAsync(command.CustomerId);
        if (customer == null)
            throw new InvalidOperationException($"Customer {command.CustomerId} not found");

        var domainId = command.DomainId ?? _domainConfig.GetDefaultDomainId();

        var ticket = Ticket.CreateFromPortal(
            command.Description,
            command.CustomerId,
            priorityScore: null,
            tags: null,
            completionTarget: command.CompletionTarget);

        ticket.DomainId = domainId;
        ticket.WorkItemTypeCode = command.WorkItemTypeCode;
        ticket.ProjectGuid = command.ProjectGuid;

        // Parse custom fields into JSON
        if (command.CustomFields.Any())
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

    public async Task UpdateAsync(Ticket ticket, UpdateTicketCommand command, CancellationToken ct)
    {
        ticket.UpdateDescription(command.Description, command.ModifiedByUserId);
        ticket.UpdateTitle(command.Description.Length > 50
            ? command.Description[..47] + "..."
            : command.Description, command.ModifiedByUserId);

        ticket.CompletionTarget = command.CompletionTarget;
        ticket.CustomerId = command.CustomerId;
        ticket.ProjectGuid = command.ProjectGuid;

        // Handle status transition if changed
        if (ticket.TicketStatus.ToString() != command.TicketStatus)
        {
            if (Enum.TryParse<Domain.Common.Status>(command.TicketStatus, out var newStatus))
            {
                ticket.TransitionTo(newStatus, command.ModifiedByUserId);
            }
        }

        if (command.CustomFields.Any())
        {
            ticket.UpdateCustomFields(
                System.Text.Json.JsonSerializer.Serialize(command.CustomFields),
                command.ModifiedByUserId);
        }

        await _ticketRepository.UpdateAsync(ticket);
    }

    public async Task AssignAsync(Ticket ticket, AssignTicketCommand command, CancellationToken ct)
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

    public async Task TransitionStatusAsync(Ticket ticket, TransitionStatusCommand command, CancellationToken ct)
    {
        if (!Enum.TryParse<Domain.Common.Status>(command.ToStatus, out var targetStatus))
            throw new InvalidOperationException($"Invalid status: {command.ToStatus}");

        ticket.TransitionTo(targetStatus, command.ChangedByUserId);
        await _ticketRepository.UpdateAsync(ticket);
    }
}
