namespace TicketMasala.Web.Engine.GERDA.Dispatching.Configuration;

/// <summary>
/// Configuration for WSJF (Weighted Shortest Job First) algorithm.
/// Used by both TicketMasala and Atom.
/// </summary>
public class WsjfConfig
{
    /// <summary>Weight for business value component (0-1)</summary>
    public decimal BusinessValueWeight { get; set; } = 0.4m;

    /// <summary>Weight for time criticality component (0-1)</summary>
    public decimal TimeCriticalityWeight { get; set; } = 0.35m;

    /// <summary>Weight for risk reduction component (0-1)</summary>
    public decimal RiskReductionWeight { get; set; } = 0.25m;

    /// <summary>Default job size (story points) when not specified</summary>
    public int DefaultJobSizePoints { get; set; } = 5;

    /// <summary>Days threshold for critical urgency (21+ days = critical)</summary>
    public int DaysUntilCritical { get; set; } = 21;

    /// <summary>Days threshold for high urgency (14+ days = high)</summary>
    public int DaysUntilHigh { get; set; } = 14;

    /// <summary>Days threshold for medium urgency (7+ days = medium)</summary>
    public int DaysUntilMedium { get; set; } = 7;

    /// <summary>Normalizer for financial value to 0-100 scale</summary>
    public decimal FinancialValueNormalizer { get; set; } = 100000m;

    /// <summary>WSJF score threshold for high urgency classification</summary>
    public decimal HighUrgencyThreshold { get; set; } = 5m;

    /// <summary>WSJF score threshold for medium urgency classification</summary>
    public decimal MediumUrgencyThreshold { get; set; } = 2m;
}

/// <summary>
/// Configuration for agent matching/dispatching.
/// </summary>
public class DispatchingConfig
{
    /// <summary>Weight for skill match factor (0-1)</summary>
    public decimal SkillMatchWeight { get; set; } = 0.4m;

    /// <summary>Weight for workload balance factor (0-1)</summary>
    public decimal WorkloadBalanceWeight { get; set; } = 0.3m;

    /// <summary>Weight for affinity/historical factor (0-1)</summary>
    public decimal AffinityWeight { get; set; } = 0.2m;

    /// <summary>Weight for availability factor (0-1)</summary>
    public decimal AvailabilityWeight { get; set; } = 0.1m;

    /// <summary>Maximum cases an agent can handle</summary>
    public int MaxCasesPerAgent { get; set; } = 15;

    /// <summary>Optimal utilization threshold (0-1)</summary>
    public decimal OptimalUtilizationThreshold { get; set; } = 0.7m;

    /// <summary>Confidence threshold for assignments (%)</summary>
    public decimal ConfidenceThreshold { get; set; } = 70m;
}
