namespace TicketMasala.Web.Engine.GERDA.Dispatching;

/// <summary>
/// Deep module entry point for all ticket dispatching operations.
///
/// Hides agent scoring, ranking, auto-dispatch policy, and model retraining.
/// Callers provide a command; the module guarantees consistent scoring invariants.
///
/// Invariants enforced internally:
/// 1. Load ticket and agent pool
/// 2. Calculate multi-factor scores (ML affinity, workload, skill, geo, language)
/// 3. Rank and return top N agents
/// 4. For auto-dispatch: check policy threshold, then delegate assignment to ITicketLifecycle
/// 5. For retrain: rebuild ML model from completed ticket history
/// </summary>
public interface ITicketDispatcher
{
    /// <summary>
    /// True if dispatching is enabled in configuration.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Execute a dispatch command.
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Structured result — never throws on domain validation</returns>
    Task<DispatcherResult> ExecuteAsync(
        IDispatchCommand command,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Marker interface for all dispatch commands.
/// </summary>
public interface IDispatchCommand { }

/// <summary>
/// Get top N agent recommendations for a ticket.
/// </summary>
public sealed record RecommendAgentsCommand(
    Guid TicketGuid,
    int Count = 3
) : IDispatchCommand;

/// <summary>
/// Auto-dispatch a ticket to the best available agent if score exceeds policy threshold.
/// Internally delegates assignment to ITicketLifecycle.
/// </summary>
public sealed record AutoDispatchCommand(
    Guid TicketGuid,
    double? MinimumScore = null
) : IDispatchCommand;

/// <summary>
/// Retrain the affinity ML model from completed ticket history.
/// </summary>
public sealed record RetrainCommand : IDispatchCommand;
