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

    Task<TicketResult<Guid>> CreateAsync(CreateTicketCommand command, CancellationToken ct = default);
    Task<TicketResult<Unit>> UpdateAsync(UpdateTicketCommand command, CancellationToken ct = default);
    Task<TicketResult<Unit>> AssignAsync(AssignTicketCommand command, CancellationToken ct = default);
    Task<TicketResult<Unit>> TransitionStatusAsync(TransitionStatusCommand command, CancellationToken ct = default);

    // ─── Query operations ────────────────────────────────────────────────────

    Task<TicketResult<TicketDetailsDto>> GetDetailsAsync(Guid ticketId, string requestingUserId, IEnumerable<string> requestingUserRoles, CancellationToken ct = default);
    Task<TicketSearchResult> SearchAsync(TicketSearchQuery query, CancellationToken ct = default);

    // ─── UI context (read operations for views) ──────────────────────────────
    // Note: These methods don't accept CancellationToken because the underlying
    // view services (ITicketReadService, ITicketContextFacade) don't support it.
    // Adding ct parameters would give callers false impression of cancellation support.

    /// <summary>Full search including saved filters and role-based customer scoping.</summary>
    Task<TicketSearchViewModel> SearchForUiAsync(TicketSearchViewModel searchModel, ClaimsPrincipal user);

    /// <summary>Detail page view model + domain context for polymorphic UI.</summary>
    Task<(TicketDetailsViewModel? ViewModel, TicketDetailContext Context)> GetDetailPageAsync(Guid ticketId, ClaimsPrincipal user);

    /// <summary>AI-generated summary for a ticket.</summary>
    Task<string> GenerateAiSummaryAsync(Guid ticketId);

    /// <summary>Lists and domain config for the ticket creation form.</summary>
    Task<TicketCreateContext> GetCreateContextAsync(Guid? projectGuid, ClaimsPrincipal user);

    /// <summary>Full edit context including valid status transitions and custom field values.</summary>
    Task<TicketEditContext?> GetEditContextAsync(Guid ticketId, ClaimsPrincipal user);

    /// <summary>Reloads edit context when form validation fails (minimal set of lists + domain config).</summary>
    Task<TicketCreateContext> GetCreateReloadContextAsync(Guid? projectGuid, ClaimsPrincipal user);

    /// <summary>Reloads edit context on failure with valid statuses and field values.</summary>
    Task<TicketEditContext> GetEditReloadContextAsync(Guid ticketId, ClaimsPrincipal user);
}

// Result type for explicit success/failure
public record TicketResult<T>
{
    public bool IsSuccess { get; init; }
    public T Value { get; init; } = default!;
    public string ErrorMessage { get; init; } = string.Empty;
    public static TicketResult<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static TicketResult<T> Failure(string message) => new() { IsSuccess = false, ErrorMessage = message };
}

public record Unit { public static Unit Value = new(); }
