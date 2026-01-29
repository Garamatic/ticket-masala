namespace TicketMasala.Dispatch.Common.Models;

/// <summary>
/// Urgency classification levels.
/// </summary>
public enum UrgencyLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Result of WSJF prioritization calculation.
/// Generic across any work item type.
/// </summary>
public class PrioritizationResult
{
    /// <summary>Work item identifier</summary>
    public string WorkItemId { get; set; } = string.Empty;

    /// <summary>Calculated priority score</summary>
    public decimal PriorityScore { get; set; }

    /// <summary>Cost of delay component</summary>
    public decimal CostOfDelay { get; set; }

    /// <summary>Job size in points</summary>
    public decimal JobSizePoints { get; set; }

    /// <summary>WSJF score (CostOfDelay / JobSize)</summary>
    public decimal WsjfScore { get; set; }

    /// <summary>Classified urgency level</summary>
    public UrgencyLevel UrgencyLevel { get; set; }

    /// <summary>Timestamp of calculation</summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Breakdown of scoring factors for transparency</summary>
    public Dictionary<string, decimal> ScoreBreakdown { get; set; } = new();
}

/// <summary>
/// Result of dispatch (agent matching) operation.
/// Generic across any work item type.
/// </summary>
public class DispatchResult
{
    /// <summary>Work item identifier</summary>
    public string WorkItemId { get; set; } = string.Empty;

    /// <summary>Recommended agent ID</summary>
    public string? RecommendedAgentId { get; set; }

    /// <summary>Overall match score (0-100)</summary>
    public decimal MatchScore { get; set; }

    /// <summary>Breakdown of scoring factors (for transparency)</summary>
    public Dictionary<string, decimal> ScoreBreakdown { get; set; } = new();

    /// <summary>Reason for decision (human-readable)</summary>
    public string Rationale { get; set; } = string.Empty;

    /// <summary>Error message if dispatch failed</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Is this a confident recommendation?</summary>
    public bool IsConfident => MatchScore >= 70m;

    /// <summary>Timestamp of dispatch decision</summary>
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Complete dispatch pipeline result combining prioritization and dispatching.
/// </summary>
public class DispatchPipelineResult
{
    /// <summary>Work item identifier</summary>
    public string WorkItemId { get; set; } = string.Empty;

    /// <summary>Overall success status</summary>
    public bool IsSuccessful { get; set; }

    /// <summary>Error message if pipeline failed</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Prioritization output</summary>
    public PrioritizationResult? PrioritizationResult { get; set; }

    /// <summary>Dispatch output</summary>
    public DispatchResult? DispatchResult { get; set; }
}
