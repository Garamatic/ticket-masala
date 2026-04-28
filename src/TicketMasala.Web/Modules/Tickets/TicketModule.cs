using System.Security.Claims;
using TicketMasala.Web.AI;
using TicketMasala.Web.Engine.Core;
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

    public async Task<Common.Result<Guid>> CreateAsync(CreateTicketCommand command, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Creating ticket for customer {CustomerId}", command.CustomerId);

            // Create ticket - GERDA processing is now handled by TicketCreatedGerdaHandler
            // which is dispatched via DomainEventDispatchingInterceptor after successful save.
            var ticket = await _lifecycle.CreateAsync(command, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Ticket {TicketGuid} created. GERDA processing queued via domain event handler.",
                ticket.Guid);

            return Common.Result.Success(ticket.Guid);
        }
        catch (TicketMasala.Domain.Exceptions.DomainException ex)
        {
            // Domain exceptions are safe to expose to users (validation errors, business rule violations)
            _logger.LogWarning(ex, "Domain validation failed during ticket creation");
            return Common.Result.Failure<Guid>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            // InvalidOperationException from repository or service layer - safe to expose
            _logger.LogWarning(ex, "Invalid operation during ticket creation");
            return Common.Result.Failure<Guid>(ex.Message);
        }
        catch (Exception ex)
        {
            // Unexpected errors - log full details but return generic message
            _logger.LogError(ex, "Unexpected error creating ticket for customer {CustomerId}", command.CustomerId);
            return Common.Result.Failure<Guid>("An unexpected error occurred while creating the ticket. Please try again or contact support.");
        }
    }

    public async Task<Common.Result<Unit>> UpdateAsync(UpdateTicketCommand command, CancellationToken ct)
    {
        var ticket = await _queries.GetByIdAsync(command.TicketId, includeRelations: false, ct).ConfigureAwait(false);
        if (ticket == null)
            return Common.Result.Failure<Unit>("Ticket not found");

        if (!_auth.CanEdit(ticket, command.ModifiedByUserId, command.ModifiedByRoles))
            return Common.Result.Failure<Unit>("Not authorized to edit this ticket");

        try
        {
            await _lifecycle.UpdateAsync(ticket, command, ct).ConfigureAwait(false);
            return Common.Result.Success(Unit.Value);
        }
        catch (TicketMasala.Domain.Exceptions.DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed during ticket update");
            return Common.Result.Failure<Unit>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during ticket update");
            return Common.Result.Failure<Unit>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating ticket {TicketId}", command.TicketId);
            return Common.Result.Failure<Unit>("An unexpected error occurred while updating the ticket. Please try again or contact support.");
        }
    }

    public async Task<Common.Result<Unit>> AssignAsync(AssignTicketCommand command, CancellationToken ct)
    {
        var ticket = await _queries.GetByIdAsync(command.TicketId, includeRelations: false, ct).ConfigureAwait(false);
        if (ticket == null)
            return Common.Result.Failure<Unit>("Ticket not found");

        if (!_auth.CanAssign(ticket, command.AssignedByUserId, command.AssignedByRoles))
            return Common.Result.Failure<Unit>("Not authorized to assign tickets");

        try
        {
            await _lifecycle.AssignAsync(ticket, command, ct).ConfigureAwait(false);
            return Common.Result.Success(Unit.Value);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during ticket assignment");
            return Common.Result.Failure<Unit>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error assigning ticket {TicketId}", command.TicketId);
            return Common.Result.Failure<Unit>("An unexpected error occurred while assigning the ticket. Please try again or contact support.");
        }
    }

    public async Task<Common.Result<Unit>> TransitionStatusAsync(TransitionStatusCommand command, CancellationToken ct)
    {
        var ticket = await _queries.GetByIdAsync(command.TicketId, includeRelations: false, ct).ConfigureAwait(false);
        if (ticket == null)
            return Common.Result.Failure<Unit>("Ticket not found");

        if (!_auth.CanChangeStatus(ticket, command.ChangedByUserId, command.ChangedByRoles, command.ToStatus))
            return Common.Result.Failure<Unit>("Not authorized to change ticket status");

        try
        {
            await _lifecycle.TransitionStatusAsync(ticket, command, ct).ConfigureAwait(false);
            return Common.Result.Success(Unit.Value);
        }
        catch (TicketMasala.Domain.Exceptions.DomainException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed during status transition");
            return Common.Result.Failure<Unit>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during status transition");
            return Common.Result.Failure<Unit>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error transitioning ticket {TicketId} status", command.TicketId);
            return Common.Result.Failure<Unit>("An unexpected error occurred while changing the ticket status. Please try again or contact support.");
        }
    }

    public async Task<Common.Result<TicketDetailsDto>> GetDetailsAsync(Guid ticketId, string requestingUserId, IEnumerable<string> requestingUserRoles, CancellationToken ct)
    {
        var ticket = await _queries.GetByIdAsync(ticketId, includeRelations: true, ct).ConfigureAwait(false);
        if (ticket == null)
            return Common.Result.Failure<TicketDetailsDto>("Ticket not found");

        if (!_auth.CanView(ticket, requestingUserId, requestingUserRoles))
            return Common.Result.Failure<TicketDetailsDto>("Not authorized to view this ticket");

        var dto = TicketDetailsDtoFactory.Create(
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
            Ticket.GetValidTransitions(ticket.TicketStatus));

        return Common.Result.Success(dto);
    }

    public async Task<TicketSearchResult> SearchAsync(TicketSearchQuery query, CancellationToken ct)
    {
        return await _queries.SearchAsync(query, ct).ConfigureAwait(false);
    }

    // --- UI context methods ----------------------------------------------------

    public async Task<TicketSearchViewModel> SearchForUiAsync(TicketSearchViewModel searchModel, ClaimsPrincipal user)
    {
        searchModel ??= new TicketSearchViewModel();

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = user.IsInRole(Constants.RoleCustomer);

        // Apply customer filter for customer users
        if (isCustomer && !string.IsNullOrEmpty(userId))
        {
            searchModel.CustomerId = userId;
        }

        // Use read service for full search with select lists
        var result = await _readService.SearchTicketsAsync(searchModel).ConfigureAwait(false);

        // Load saved filters
        if (!string.IsNullOrEmpty(userId))
        {
            result.SavedFilters = await _savedFilterService.GetFiltersForUserAsync(userId).ConfigureAwait(false);
        }

        return result;
    }

    public async Task<(TicketDetailsViewModel? ViewModel, TicketDetailContext Context)> GetDetailPageAsync(Guid ticketId, ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var isCustomer = user.IsInRole(Constants.RoleCustomer);

        // Get user roles for authorization
        var userRoles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        // Check existence and authorization (fail fast before loading view model)
        var ticket = await _queries.GetByIdAsync(ticketId, includeRelations: false, CancellationToken.None).ConfigureAwait(false);
        if (ticket == null)
        {
            return (null, new TicketDetailContext());
        }

        if (!_auth.CanView(ticket, userId, userRoles))
        {
            throw new UnauthorizedAccessException("Not authorized to view this ticket");
        }

        // Load view model for authorized user
        var viewModel = await _contextFacade.GetTicketDetailsAsync(ticketId, userId, isCustomer).ConfigureAwait(false);
        if (viewModel == null)
        {
            return (null, new TicketDetailContext());
        }

        // Get domain context
        var context = await _contextFacade.GetTicketDetailContextAsync(viewModel).ConfigureAwait(false);

        return (viewModel, context);
    }

    public async Task<string> GenerateAiSummaryAsync(Guid ticketId, ClaimsPrincipal user)
    {
        // Get user info for authorization
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var userRoles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        var isCustomer = user.IsInRole(Constants.RoleCustomer);

        // Get ticket to check authorization
        var ticket = await _queries.GetByIdAsync(ticketId, includeRelations: false, CancellationToken.None).ConfigureAwait(false);
        if (ticket == null)
        {
            throw new ArgumentException("Ticket not found");
        }

        if (!_auth.CanView(ticket, userId, userRoles))
        {
            throw new UnauthorizedAccessException("Not authorized to view this ticket");
        }

        // Use actual user context for fetching details and generating summary
        return await GenerateAiSummaryWithDetailsAsync(ticketId, userId, isCustomer).ConfigureAwait(false);
    }

    public Task<string> GenerateAiSummaryAsync(Guid ticketId)
    {
        // Legacy overload without user context - for backward compatibility only
        // Uses unfiltered access (assumes caller has already authorized)
        return GenerateAiSummaryWithDetailsAsync(ticketId, userId: string.Empty, isCustomer: false);
    }

    private async Task<string> GenerateAiSummaryWithDetailsAsync(Guid ticketId, string userId, bool isCustomer)
    {
        var viewModel = await _contextFacade.GetTicketDetailsAsync(ticketId, userId, isCustomer).ConfigureAwait(false);

        if (viewModel == null)
        {
            throw new ArgumentException("Ticket not found");
        }

        return await BuildAndSendAiSummaryAsync(viewModel).ConfigureAwait(false);
    }

    private async Task<string> BuildAndSendAiSummaryAsync(TicketDetailsViewModel viewModel)
    {
        // Build query for AI
        var commentLines = viewModel.Comments
            .OrderBy(c => c.CreatedAt)
            .Select(c => $"- {(c.Author?.Name ?? c.Author?.UserName ?? "Unknown")} ({c.CreatedAt}): {c.Body}");

        var query = $"Title: {viewModel.Description} (Created: {viewModel.CreationDate})\n\n" +
                $"Status: {viewModel.TicketStatus}\n\n" +
                "Discussion:\n" +
                string.Join("\n", commentLines);

        return await _openAiService.GetResponseAsync(OpenAIPrompts.Summary, query).ConfigureAwait(false);
    }

    public async Task<TicketCreateContext> GetCreateContextAsync(Guid? projectGuid, ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isCustomer = user.IsInRole(Constants.RoleCustomer);

        return await _contextFacade.GetCreateContextAsync(isCustomer, userId, projectGuid).ConfigureAwait(false);
    }

    public async Task<TicketEditContext?> GetEditContextAsync(Guid ticketId, ClaimsPrincipal user)
    {
        return await _contextFacade.GetEditContextAsync(ticketId, user).ConfigureAwait(false);
    }

    public Task<TicketCreateContext> GetCreateReloadContextAsync(Guid? projectGuid, ClaimsPrincipal user)
        => GetCreateContextAsync(projectGuid, user);

    public async Task<TicketEditContext> GetEditReloadContextAsync(Guid ticketId, ClaimsPrincipal user)
    {
        return await _contextFacade.GetEditReloadContextAsync(ticketId, user).ConfigureAwait(false);
    }
}
