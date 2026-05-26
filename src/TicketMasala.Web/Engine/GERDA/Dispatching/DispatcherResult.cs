namespace TicketMasala.Web.Engine.GERDA.Dispatching;

/// <summary>
/// Unified result for all dispatch operations.
/// </summary>
public sealed record DispatcherResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Human-readable error message when Success is false.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Ranked agent recommendations (populated for RecommendAgentsCommand).
    /// </summary>
    public IReadOnlyList<AgentRecommendation> Recommendations { get; init; } = Array.Empty<AgentRecommendation>();

    /// <summary>
    /// Whether the ticket was auto-assigned (populated for AutoDispatchCommand).
    /// </summary>
    public bool WasAutoAssigned { get; init; }

    /// <summary>
    /// The assigned agent ID if WasAutoAssigned is true.
    /// </summary>
    public string? AssignedAgentId { get; init; }

    /// <summary>
    /// Timestamp of last model training (populated for RetrainCommand).
    /// </summary>
    public DateTime? LastTrained { get; init; }

    public static DispatcherResult WithRecommendations(IReadOnlyList<AgentRecommendation> recommendations)
        => new() { Success = true, Recommendations = recommendations };

    public static DispatcherResult AutoAssigned(string agentId)
        => new() { Success = true, WasAutoAssigned = true, AssignedAgentId = agentId };

    public static DispatcherResult Skipped(string reason)
        => new() { Success = true, WasAutoAssigned = false, ErrorMessage = reason };

    public static DispatcherResult Fail(string error)
        => new() { Success = false, ErrorMessage = error };

    public static DispatcherResult Retrained(DateTime? lastTrained)
        => new() { Success = true, LastTrained = lastTrained };
}

/// <summary>
/// A single agent recommendation with score and explanatory reasons.
/// </summary>
public sealed record AgentRecommendation
{
    public required string AgentId { get; init; }
    public required double Score { get; init; }
    public required List<string> Reasons { get; init; } = new();
    public string? Explanation { get; init; }
}
