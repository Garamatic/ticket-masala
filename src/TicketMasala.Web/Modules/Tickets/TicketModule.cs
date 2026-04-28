using System.Security.Claims;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.AI;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Facades;
using TicketMasala.Web.Modules.Tickets.Internal;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Modules.Tickets;

/// <summary>
/// Deep module implementation for ticket operations.
/// Consolidates lifecycle, query, authorization, and UI context services.
/// </summary>
internal class TicketModule : ITicketModule
{
    private readonly ITicketLifecycleService _lifecycle;
    private readonly ITicketQueryService _queries;
    private readonly ITicketAuthorizationService _auth;
    private readonly ITicketContextFacade _contextFacade;
    private readonly ITicketReadService _readService;
    private readonly ISavedFilterService _savedFilterService;
    private readonly IOpenAiService _openAiService;
    private readonly ILogger<TicketModule> _logger;

    public TicketModule(
        ITicketLifecycleService lifecycle,
        ITicketQueryService queries,
        ITicketAuthorizationService auth,
        ITicketContextFacade contextFacade,
        ITicketReadService readService,
        ISavedFilterService savedFilterService,
        IOpenAiService openAiService,
        ILogger<TicketModule> logger)
    {
        _lifecycle = lifecycle;
        _queries = queries;
        _auth = auth;
        _contextFacade = contextFacade;
        _readService = readService;
        _savedFilterService = savedFilterService;
        _openAiService = openAiService;
        _logger = logger;
    }

    // --- Core lifecycle -------------------------------------------------------

    public async Task<TicketResult<Guid>> CreateAsync(CreateTicketCommand command, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Creating ticket for customer {CustomerId}", command.CustomerId ?? "(null)");

            // Create ticket - GERDA processing is now handled by TicketCreatedGerdaHandler
            // which is dispatched via DomainEventDispatchingInterceptor after successful save.
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

    // --- UI context methods ----------------------------------------------------

    public async Task<TicketSearchViewModel> SearchForUiAsync(TicketSearchViewModel searchModel, ClaimsPrincipal user, CancellationToken ct)
    {
        if (searchModel == null)
            searchModel = new TicketSearchViewModel();

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = user.IsInRole(Constants.RoleCustomer);

        // Apply customer filter for customer users
        if (isCustomer && !string.IsNullOrEmpty(userId))
        {
            searchModel.CustomerId = userId;
        }

        // Use read service for full search with select lists
        var result = await _readService.SearchTicketsAsync(searchModel);

        // Load saved filters
        if (!string.IsNullOrEmpty(userId))
        {
            result.SavedFilters = await _savedFilterService.GetFiltersForUserAsync(userId);
        }

        return result;
    }

    public async Task<(TicketDetailsViewModel? ViewModel, TicketDetailContext Context)> GetDetailPageAsync(Guid ticketId, ClaimsPrincipal user, CancellationToken ct)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = user.IsInRole(Constants.RoleCustomer);

        try
        {
            // Get view model from facade
            var viewModel = await _contextFacade.GetTicketDetailsAsync(ticketId, userId, isCustomer);
            if (viewModel == null)
            {
                return (null, new TicketDetailContext());
            }

            // Get domain context
            var context = await _contextFacade.GetTicketDetailContextAsync(viewModel);

            return (viewModel, context);
        }
        catch (UnauthorizedAccessException)
        {
            // Re-throw to let controller handle with Forbid()
            throw;
        }
    }

    public async Task<string> GenerateAiSummaryAsync(Guid ticketId, CancellationToken ct)
    {
        // Get ticket details
        var userId = string.Empty;
        var isCustomer = false;
        var viewModel = await _contextFacade.GetTicketDetailsAsync(ticketId, userId, isCustomer);

        if (viewModel == null)
        {
            throw new ArgumentException("Ticket not found");
        }

        // Build query for AI
        var query = $"Title: {viewModel.Description} (Created: {viewModel.CreationDate})\n\n" +
                $"Status: {viewModel.TicketStatus}\n\n" +
                $"Discussion:\n" +
                string.Join("\n", viewModel.Comments.OrderBy(c => c.CreatedAt).Select(c =>
                    $"- {c.Author?.Name ?? c.Author?.UserName ?? "Unknown"} ({c.CreatedAt}): {c.Body}"));

        return await _openAiService.GetResponseAsync(OpenAIPrompts.Summary, query);
    }

    public async Task<TicketCreateContext> GetCreateContextAsync(Guid? projectGuid, ClaimsPrincipal user, CancellationToken ct)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = user.IsInRole(Constants.RoleCustomer);

        return await _contextFacade.GetCreateContextAsync(isCustomer, userId, projectGuid);
    }

    public async Task<TicketEditContext?> GetEditContextAsync(Guid ticketId, ClaimsPrincipal user, CancellationToken ct)
    {
        return await _contextFacade.GetEditContextAsync(ticketId, user);
    }

    public Task<TicketCreateContext> GetCreateReloadContextAsync(Guid? projectGuid, ClaimsPrincipal user, CancellationToken ct)
    {
        // Reload uses same context as initial load
        return GetCreateContextAsync(projectGuid, user, ct);
    }

    public async Task<TicketEditContext> GetEditReloadContextAsync(Guid ticketId, ClaimsPrincipal user, CancellationToken ct)
    {
        return await _contextFacade.GetEditReloadContextAsync(ticketId, user);
    }
}
