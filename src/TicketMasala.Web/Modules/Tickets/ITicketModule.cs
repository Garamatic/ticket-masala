namespace TicketMasala.Web.Modules.Tickets;

public interface ITicketModule
{
    // Core lifecycle
    Task<TicketResult<Guid>> CreateAsync(CreateTicketCommand command, CancellationToken ct = default);
    Task<TicketResult<Unit>> UpdateAsync(UpdateTicketCommand command, CancellationToken ct = default);
    Task<TicketResult<Unit>> AssignAsync(AssignTicketCommand command, CancellationToken ct = default);
    Task<TicketResult<Unit>> TransitionStatusAsync(TransitionStatusCommand command, CancellationToken ct = default);

    // Query (read-only, returns DTOs not entities)
    Task<TicketResult<TicketDetailsDto>> GetDetailsAsync(Guid ticketId, string requestingUserId, IEnumerable<string> requestingUserRoles, CancellationToken ct = default);
    Task<TicketSearchResult> SearchAsync(TicketSearchQuery query, CancellationToken ct = default);

    // This is the only public surface - everything else is internal
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
