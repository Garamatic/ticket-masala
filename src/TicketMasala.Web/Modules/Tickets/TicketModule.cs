using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Engine.GERDA;
using TicketMasala.Web.Facades;
using TicketMasala.Web.Modules.Tickets.Internal;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Modules.Tickets;

internal class TicketModule : ITicketModule
{
    private readonly ITicketLifecycleService _lifecycle;
    private readonly ITicketQueryService _queries;
    private readonly ITicketAuthorizationService _auth;
    private readonly ILogger<TicketModule> _logger;

    public TicketModule(
        ITicketLifecycleService lifecycle,
        ITicketQueryService queries,
        ITicketAuthorizationService auth,
        ILogger<TicketModule> logger)
    {
        _lifecycle = lifecycle;
        _queries = queries;
        _auth = auth;
        _logger = logger;
    }

    public async Task<TicketResult<Guid>> CreateAsync(CreateTicketCommand command, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Creating ticket for customer {CustomerId}", command.CustomerId);

            // Create ticket - GERDA processing is now handled by TicketCreatedGerdaHandler
            // which is dispatched via DomainEventDispatchingInterceptor after successful save.
            // This replaces the fire-and-forget Task.Run pattern with proper background queue.
            var ticket = await _lifecycle.CreateAsync(command, ct);

            _logger.LogInformation(
                "Ticket {TicketGuid} created. GERDA processing queued via domain event handler.",
                ticket.Guid);

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
            return TicketResult<Unit>.Failure($"Failed to update ticket: {ex.Message}");
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
            return TicketResult<Unit>.Failure($"Failed to assign ticket: {ex.Message}");
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
            return TicketResult<Unit>.Failure($"Failed to change ticket status: {ex.Message}");
        }
    }

    public async Task<TicketResult<TicketDetailsDto>> GetDetailsAsync(Guid ticketId, string requestingUserId, IEnumerable<string> requestingUserRoles, CancellationToken ct)
    {
        var ticket = await _queries.GetByIdAsync(ticketId, includeRelations: true, ct);
        if (ticket == null)
            return TicketResult<TicketDetailsDto>.Failure("Ticket not found");

        if (!_auth.CanView(ticket, requestingUserId, requestingUserRoles.ToList()))
            return TicketResult<TicketDetailsDto>.Failure("Not authorized to view this ticket");

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

    // ─── UI context methods (P1: Migrate from ITicketOrchestrator) ───────────
    // These methods provide the UI-specific context needed by controllers.
    // They will replace the orchestrator calls once fully implemented.

    public Task<TicketSearchViewModel> SearchForUiAsync(TicketSearchViewModel searchModel, ClaimsPrincipal user, CancellationToken ct)
    {
        // P1: Migrate from ITicketOrchestrator.SearchTicketsAsync
        throw new NotImplementedException("SearchForUiAsync is planned for P1. Use ITicketOrchestrator for now.");
    }

    public Task<(TicketDetailsViewModel? ViewModel, TicketDetailContext Context)> GetDetailPageAsync(Guid ticketId, ClaimsPrincipal user, CancellationToken ct)
    {
        // P1: Migrate from ITicketOrchestrator.GetTicketDetailsAsync + GetTicketDetailContextAsync
        throw new NotImplementedException("GetDetailPageAsync is planned for P1. Use ITicketOrchestrator for now.");
    }

    public Task<string> GenerateAiSummaryAsync(Guid ticketId, CancellationToken ct)
    {
        // P1: Migrate from ITicketOrchestrator.GenerateAiSummaryAsync
        throw new NotImplementedException("GenerateAiSummaryAsync is planned for P1. Use ITicketOrchestrator for now.");
    }

    public Task<TicketCreateContext> GetCreateContextAsync(Guid? projectGuid, ClaimsPrincipal user, CancellationToken ct)
    {
        // P1: Migrate from ITicketOrchestrator.GetCreateContextAsync
        throw new NotImplementedException("GetCreateContextAsync is planned for P1. Use ITicketOrchestrator for now.");
    }

    public Task<TicketEditContext?> GetEditContextAsync(Guid ticketId, ClaimsPrincipal user, CancellationToken ct)
    {
        // P1: Migrate from ITicketOrchestrator.GetEditContextAsync
        throw new NotImplementedException("GetEditContextAsync is planned for P1. Use ITicketOrchestrator for now.");
    }

    public Task<TicketCreateContext> GetCreateReloadContextAsync(Guid? projectGuid, ClaimsPrincipal user, CancellationToken ct)
    {
        // P1: Migrate from ITicketOrchestrator.GetCreateReloadContextAsync
        throw new NotImplementedException("GetCreateReloadContextAsync is planned for P1. Use ITicketOrchestrator for now.");
    }

    public Task<TicketEditContext> GetEditReloadContextAsync(Guid ticketId, ClaimsPrincipal user, CancellationToken ct)
    {
        // P1: Migrate from ITicketOrchestrator.GetEditReloadContextAsync
        throw new NotImplementedException("GetEditReloadContextAsync is planned for P1. Use ITicketOrchestrator for now.");
    }
}
