using Microsoft.Extensions.DependencyInjection;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Engine.GERDA;
using TicketMasala.Web.Modules.Tickets.Internal;

namespace TicketMasala.Web.Modules.Tickets;

internal class TicketModule : ITicketModule
{
    private readonly ITicketLifecycleService _lifecycle;
    private readonly ITicketQueryService _queries;
    private readonly ITicketAuthorizationService _auth;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TicketModule> _logger;

    // This is the ONLY constructor - 5 dependencies, all module-internal or cross-module interfaces
    public TicketModule(
        ITicketLifecycleService lifecycle,
        ITicketQueryService queries,
        ITicketAuthorizationService auth,
        IServiceScopeFactory scopeFactory,
        ILogger<TicketModule> logger)
    {
        _lifecycle = lifecycle;
        _queries = queries;
        _auth = auth;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<TicketResult<Guid>> CreateAsync(CreateTicketCommand command, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Creating ticket for customer {CustomerId}", command.CustomerId);

            var ticket = await _lifecycle.CreateAsync(command, ct);

            // Trigger GERDA processing (fire and forget - module handles side effects)
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var gerda = scope.ServiceProvider.GetRequiredService<IGerda>();
                    await gerda.ProcessAsync(ticket.Guid);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GERDA processing failed for ticket {TicketId}", ticket.Guid);
                }
            });

            return TicketResult<Guid>.Success(ticket.Guid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create ticket");
            return TicketResult<Guid>.Failure($"Failed to create ticket: {ex.Message}");
        }
    }

    public async Task<TicketResult<Unit>> UpdateAsync(UpdateTicketCommand command, CancellationToken ct)
    {
        var ticket = await _queries.GetByIdAsync(command.TicketId, includeRelations: false, ct);
        if (ticket == null)
            return TicketResult<Unit>.Failure("Ticket not found");

        if (!_auth.CanEdit(ticket, command.ModifiedByUserId, command.ModifiedByRoles))
            return TicketResult<Unit>.Failure("Not authorized to edit this ticket");

        try
        {
            await _lifecycle.UpdateAsync(ticket, command, ct);
            return TicketResult<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update ticket {TicketId}", command.TicketId);
            return TicketResult<Unit>.Failure(ex.Message);
        }
    }

    public async Task<TicketResult<Unit>> AssignAsync(AssignTicketCommand command, CancellationToken ct)
    {
        var ticket = await _queries.GetByIdAsync(command.TicketId, includeRelations: false, ct);
        if (ticket == null)
            return TicketResult<Unit>.Failure("Ticket not found");

        if (!_auth.CanAssign(ticket, command.AssignedByUserId, command.AssignedByRoles))
            return TicketResult<Unit>.Failure("Not authorized to assign tickets");

        try
        {
            await _lifecycle.AssignAsync(ticket, command, ct);
            return TicketResult<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign ticket {TicketId}", command.TicketId);
            return TicketResult<Unit>.Failure(ex.Message);
        }
    }

    public async Task<TicketResult<Unit>> TransitionStatusAsync(TransitionStatusCommand command, CancellationToken ct)
    {
        var ticket = await _queries.GetByIdAsync(command.TicketId, includeRelations: false, ct);
        if (ticket == null)
            return TicketResult<Unit>.Failure("Ticket not found");

        if (!_auth.CanChangeStatus(ticket, command.ChangedByUserId, command.ChangedByRoles, command.ToStatus))
            return TicketResult<Unit>.Failure("Not authorized to change ticket status");

        try
        {
            await _lifecycle.TransitionStatusAsync(ticket, command, ct);
            return TicketResult<Unit>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transition ticket {TicketId} status", command.TicketId);
            return TicketResult<Unit>.Failure(ex.Message);
        }
    }

    public async Task<TicketResult<TicketDetailsDto>> GetDetailsAsync(Guid ticketId, string requestingUserId, CancellationToken ct)
    {
        var ticket = await _queries.GetByIdAsync(ticketId, includeRelations: true, ct);
        if (ticket == null)
            return TicketResult<TicketDetailsDto>.Failure("Ticket not found");

        var dto = new TicketDetailsDto(
            ticket.Guid,
            ticket.Title,
            ticket.Description,
            ticket.Status,
            ticket.CreationDate,
            ticket.CompletionTarget,
            ticket.Responsible?.FullName,
            ticket.Customer?.FullName,
            ticket.Project?.Name,
            ticket.PriorityScore,
            ticket.GerdaTags,
            Ticket.GetValidTransitions(ticket.TicketStatus).Split(", "));

        return TicketResult<TicketDetailsDto>.Success(dto);
    }

    public async Task<TicketSearchResult> SearchAsync(TicketSearchQuery query, CancellationToken ct)
    {
        return await _queries.SearchAsync(query, ct);
    }
}
