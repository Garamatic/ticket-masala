namespace TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;

/// <summary>
/// Deep module entry point for all ticket lifecycle operations.
///
/// Hides all persistence, audit, observer notification, and event publishing choreography.
/// Callers provide a command + context; the module guarantees invariants.
///
/// Invariants enforced internally (callers cannot skip):
/// 1. Load entity from repository
/// 2. Apply domain mutation
/// 3. Queue persistence via UoW
/// 4. Queue audit log
/// 5. Commit transaction
/// 6. Notify observers (after commit)
/// 7. Publish integration events (after commit, best-effort)
/// </summary>
public interface ITicketLifecycle
{
    /// <summary>
    /// Execute a ticket lifecycle command.
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="context">Ambient execution context (user, tenant, timestamp)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Structured result — never throws on domain validation</returns>
    Task<TicketResult> ExecuteAsync(
        ITicketCommand command,
        TicketContext context,
        CancellationToken cancellationToken = default);
}
