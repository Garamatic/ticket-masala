using Microsoft.Extensions.Logging;
using TicketMasala.Web.Engine.GERDA.Dispatching.Configuration;
using TicketMasala.Web.Engine.GERDA.Dispatching.Models;

namespace TicketMasala.Web.Engine.GERDA.Dispatching.Algorithms;

/// <summary>
/// Generic WSJF (Weighted Shortest Job First) calculation engine.
/// Works with ANY IWorkItem (Ticket, TaxCase, etc.).
/// This is the SINGLE source of truth for prioritization.
/// </summary>
public class WsjfEngine
{
    private readonly WsjfConfig _config;
    private readonly ILogger<WsjfEngine> _logger;

    public WsjfEngine(WsjfConfig config, ILogger<WsjfEngine> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Calculate WSJF priority score for a work item.
    /// Formula: Priority = Cost of Delay / Job Size
    /// </summary>
    public PrioritizationResult CalculatePriority(IWorkItem workItem)
    {
        var result = new PrioritizationResult { WorkItemId = workItem.Id };

        try
        {
            // Step 1: Extract job size
            int jobSize = workItem.EstimatedJobSize ?? _config.DefaultJobSizePoints;
            if (jobSize <= 0) jobSize = _config.DefaultJobSizePoints;
            result.JobSizePoints = jobSize;

            // Step 2: Calculate weighted cost of delay components
            var businessValue = CalculateBusinessValue(workItem);
            var timeCriticality = CalculateTimeCriticality(workItem);
            var riskReduction = CalculateRiskReduction(workItem);

            // Step 3: Calculate weighted Cost of Delay
            result.CostOfDelay = (businessValue * _config.BusinessValueWeight) +
                                 (timeCriticality * _config.TimeCriticalityWeight) +
                                 (riskReduction * _config.RiskReductionWeight);

            // Step 4: Calculate WSJF Score
            result.WsjfScore = result.CostOfDelay / jobSize;
            result.PriorityScore = result.WsjfScore;

            // Step 5: Classify urgency level
            result.UrgencyLevel = ClassifyUrgency(result.WsjfScore);

            // Step 6: Build transparency breakdown
            result.ScoreBreakdown = new Dictionary<string, decimal>
            {
                { "BusinessValue", businessValue },
                { "TimeCriticality", timeCriticality },
                { "RiskReduction", riskReduction },
                { "CostOfDelay", result.CostOfDelay },
                { "JobSize", jobSize },
                { "WsjfScore", result.WsjfScore }
            };

            _logger.LogDebug(
                "WSJF: WorkItem {WorkItemId} ({WorkType}) prioritized as {UrgencyLevel} (Score={Score:F2})",
                workItem.Id, workItem.WorkType, result.UrgencyLevel, result.WsjfScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WSJF: Failed to calculate priority for {WorkItemId}", workItem.Id);
            result.UrgencyLevel = UrgencyLevel.Medium; // Safe default
        }

        return result;
    }

    /// <summary>
    /// Calculate business value (0-100 scale).
    /// </summary>
    private decimal CalculateBusinessValue(IWorkItem workItem)
    {
        if (workItem.FinancialValue <= 0) return 0m;
        var normalized = (workItem.FinancialValue / _config.FinancialValueNormalizer) * 100m;
        return Math.Min(normalized, 100m); // Cap at 100
    }

    /// <summary>
    /// Calculate time criticality based on age (0-100 scale).
    /// Uses stepped thresholds: 7, 14, 21 days.
    /// </summary>
    private decimal CalculateTimeCriticality(IWorkItem workItem)
    {
        var age = (DateTime.UtcNow - workItem.CreatedAt).TotalDays;

        return age switch
        {
            < 7 => 25m,      // 0-7 days: Low criticality
            < 14 => 50m,     // 7-14 days: Medium criticality
            < 21 => 75m,     // 14-21 days: High criticality
            _ => 100m        // 21+ days: Critical (URGENT)
        };
    }

    /// <summary>
    /// Calculate risk reduction potential (0-100).
    /// Higher risk = more potential for risk reduction.
    /// </summary>
    private decimal CalculateRiskReduction(IWorkItem workItem)
    {
        return Math.Min(workItem.RiskScore, 100m); // Use risk score directly (0-100)
    }

    /// <summary>
    /// Classify WSJF score into urgency level.
    /// </summary>
    private UrgencyLevel ClassifyUrgency(decimal wsjfScore)
    {
        return wsjfScore switch
        {
            >= 10m => UrgencyLevel.Critical,
            >= 5m => UrgencyLevel.High,
            >= 2m => UrgencyLevel.Medium,
            _ => UrgencyLevel.Low
        };
    }
}
