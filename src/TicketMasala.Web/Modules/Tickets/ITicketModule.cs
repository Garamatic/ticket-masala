using System.Security.Claims;
using TicketMasala.Domain.Configuration;
using TicketMasala.Web.Facades;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Modules.Tickets;

/// <summary>
/// Deep module for all ticket operations.
/// Consolidates lifecycle, query, and UI context behind a single interface.
/// </summary>
/// <remarks>
/// P0 Consolidation: Replaces ITicketOrchestrator. All new code should use this interface.
/// </remarks>
public interface ITicketModule
{
    // ─── Core lifecycle (write operations) ───────────────────────────────────

    Task<Common.Result<Guid>> CreateAsync(CreateTicketCommand command, CancellationToken ct = default);
    Task<Common.Result<Unit>> UpdateAsync(UpdateTicketCommand command, CancellationToken ct = default);
    Task<Common.Result<Unit>> AssignAsync(AssignTicketCommand command, CancellationToken ct = default);
    Task<Common.Result<Unit>> TransitionStatusAsync(TransitionStatusCommand command, CancellationToken ct = default);

    // ─── Query operations ────────────────────────────────────────────────────

    Task<Common.Result<TicketDetailsDto>> GetDetailsAsync(Guid ticketId, string requestingUserId, IEnumerable<string> requestingUserRoles, CancellationToken ct = default);
    Task<TicketSearchResult> SearchAsync(TicketSearchQuery query, CancellationToken ct = default);

    // ─── UI context (read operations for views) ──────────────────────────────
    // Note: CancellationToken is accepted on most methods for consistency.
    // SearchForUiAsync accepts ct for HTTP request abortion even though underlying
    // services may not fully support cancellation.

    /// <summary>Full search including saved filters and role-based customer scoping.</summary>
    Task<TicketSearchViewModel> SearchForUiAsync(TicketSearchViewModel searchModel, ClaimsPrincipal user, CancellationToken ct = default);

    /// <summary>Detail page view model + domain context for polymorphic UI.</summary>
    /// <remarks>GetTicketDetailContextAsync is synchronous (no CT needed), but ct is used for ticket loading.</remarks>
    Task<(TicketDetailsViewModel? ViewModel, TicketDetailContext Context)> GetDetailPageAsync(Guid ticketId, ClaimsPrincipal user, CancellationToken ct = default);

    /// <summary>AI-generated summary for a ticket.</summary>
    Task<string> GenerateAiSummaryAsync(Guid ticketId, ClaimsPrincipal user, CancellationToken ct = default);

    /// <summary>AI-generated summary for a ticket (DO NOT USE - obsolete, insecure).</summary>
    [Obsolete("This overload bypasses authorization. Use the ClaimsPrincipal overload.", error: true)]
    Task<string> GenerateAiSummaryAsync(Guid ticketId);

    /// <summary>Lists and domain config for the ticket creation form.</summary>
    Task<TicketCreateContext> GetCreateContextAsync(Guid? projectGuid, ClaimsPrincipal user, CancellationToken ct = default);

    /// <summary>Full edit context including valid status transitions and custom field values.</summary>
    Task<TicketEditContext?> GetEditContextAsync(Guid ticketId, ClaimsPrincipal user, CancellationToken ct = default);

    /// <summary>Reloads edit context when form validation fails (minimal set of lists + domain config).</summary>
    Task<TicketCreateContext> GetCreateReloadContextAsync(Guid? projectGuid, ClaimsPrincipal user, CancellationToken ct = default);

    /// <summary>Reloads edit context on failure with valid statuses and field values.</summary>
    Task<TicketEditContext> GetEditReloadContextAsync(Guid ticketId, ClaimsPrincipal user, CancellationToken ct = default);
}

// Unit type for operations that return no value
public record Unit
{
    private static readonly Unit _value = new();
    public static Unit Value => _value;
}
