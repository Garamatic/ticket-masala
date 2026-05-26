namespace TicketMasala.Web.Engine.GERDA;

/// <summary>
/// Deep module interface for GERDA AI processing.
/// Single entry point hiding 7+ stages and 15+ strategies.
/// </summary>
/// <remarks>
/// Design: Caller-Optimized — simple by default, powerful when needed.
/// - 90% case: await _gerda.ProcessAsync(guid)
/// - 10% case: _gerda.Configure().Stages(...).OnProgress(...).ExecuteAsync()
/// </remarks>
public interface IGerda
{
    /// <summary>
    /// The one-line default: process a ticket with all enabled stages.
    /// Returns immediately with defaults if GERDA is disabled.
    /// </summary>
    /// <param name="ticketGuid">The ticket to process</param>
    /// <returns>Immutable outcome with all stage results</returns>
    Task<GerdaOutcome> ProcessAsync(Guid ticketGuid);

    /// <summary>
    /// Advanced: configure which stages to run, get progress, customize behavior.
    /// </summary>
    /// <returns>Fluent builder for advanced scenarios</returns>
    IGerdaAdvancedBuilder Configure();

    /// <summary>
    /// True if GERDA is enabled and at least one stage is active.
    /// </summary>
    bool IsActive { get; }
}

/// <summary>
/// Immutable result of GERDA processing.
/// Null properties indicate disabled stages or no recommendation.
/// </summary>
public sealed record GerdaOutcome(
    Guid TicketGuid,
    bool WasGrouped,                      // G - Grouping: true if this is a child ticket
    double? EstimatedEffort,              // E - Estimating: effort points, null if disabled
    double? PriorityScore,                // R - Ranking: WSJF score, null if disabled
    Guid? SuggestedAgentId,               // D - Dispatching: null if no recommendation
    IReadOnlyList<Guid> RelatedArticles   // K - Knowledge: empty if disabled
)
{
    /// <summary>
    /// Names of stages that failed during processing.
    /// Empty when all stages succeeded (or were disabled).
    /// </summary>
    public IReadOnlyList<string> StageFailures { get; init; } = Array.Empty<string>();

    /// <summary>
    /// True if any stage failed during processing.
    /// Callers should check this to distinguish partial success from total success.
    /// </summary>
    public bool HasPartialFailure => StageFailures.Count > 0;

    /// <summary>
    /// Creates a default outcome for when GERDA is disabled.
    /// </summary>
    public static GerdaOutcome Disabled(Guid ticketGuid) => new(
        ticketGuid,
        WasGrouped: false,
        EstimatedEffort: null,
        PriorityScore: null,
        SuggestedAgentId: null,
        RelatedArticles: Array.Empty<Guid>()
    );
}

/// <summary>
/// GERDA processing stages. Used for advanced configuration.
/// </summary>
public enum GerdaStage
{
    Grouping,      // G - Spam detection and ticket clustering
    Estimating,    // E - Complexity estimation
    Ranking,       // R - Priority scoring (WSJF)
    Dispatching,   // D - Agent recommendation
    Knowledge,     // K - KB article suggestions
    Anticipation   // A - Capacity forecasting (batch only)
}

/// <summary>
/// Fluent builder for advanced GERDA configuration.
/// </summary>
public interface IGerdaAdvancedBuilder
{
    /// <summary>
    /// Select specific stages to run. If not called, all enabled stages run.
    /// </summary>
    IGerdaAdvancedBuilder Stages(params GerdaStage[] stages);

    /// <summary>
    /// Register a progress callback invoked after each stage.
    /// </summary>
    IGerdaAdvancedBuilder OnProgress(Action<GerdaStageProgress> progress);

    /// <summary>
    /// Set a timeout for the entire pipeline.
    /// </summary>
    IGerdaAdvancedBuilder WithTimeout(TimeSpan timeout);

    /// <summary>
    /// Execute the configured pipeline.
    /// </summary>
    Task<GerdaDetailedResult> ExecuteAsync(Guid ticketGuid, CancellationToken cancellationToken = default);
}

/// <summary>
/// Progress notification from a single stage.
/// </summary>
public sealed record GerdaStageProgress(
    GerdaStage Stage,
    Guid TicketGuid,
    TimeSpan Duration,
    bool Succeeded,
    string? ErrorMessage
);

/// <summary>
/// Detailed result including per-stage diagnostics.
/// Wraps GerdaOutcome for simple result access.
/// </summary>
public sealed record GerdaDetailedResult
{
    /// <summary>
    /// The standard outcome with all stage results.
    /// </summary>
    public required GerdaOutcome Outcome { get; init; }

    /// <summary>
    /// Per-stage execution details for debugging.
    /// </summary>
    public IReadOnlyDictionary<GerdaStage, GerdaStageDetail> StageDetails { get; init; }
        = new Dictionary<GerdaStage, GerdaStageDetail>();

    /// <summary>
    /// Total pipeline execution time.
    /// </summary>
    public TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// Execution log in order of stage completion.
    /// </summary>
    public IReadOnlyList<GerdaStageProgress> ExecutionLog { get; init; }
        = new List<GerdaStageProgress>();
}

/// <summary>
/// Detailed information about a single stage's execution.
/// </summary>
public sealed record GerdaStageDetail(
    bool WasExecuted,
    TimeSpan Duration,
    object? RawResult,
    Exception? Error
);
