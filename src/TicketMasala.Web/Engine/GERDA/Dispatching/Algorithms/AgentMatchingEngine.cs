using Microsoft.Extensions.Logging;
using TicketMasala.Web.Engine.GERDA.Dispatching.Configuration;
using TicketMasala.Web.Engine.GERDA.Dispatching.Models;
using DispatchResultModel = TicketMasala.Web.Engine.GERDA.Dispatching.Models.DispatchResult;

namespace TicketMasala.Web.Engine.GERDA.Dispatching.Algorithms;

/// <summary>
/// Generic agent matching engine based on skill, workload, affinity, and availability.
/// Works with ANY work item type (Ticket, TaxCase, etc.).
/// This is the SINGLE source of truth for agent dispatching.
/// </summary>
public class AgentMatchingEngine
{
    private readonly DispatchingConfig _config;
    private readonly ILogger<AgentMatchingEngine> _logger;

    public AgentMatchingEngine(DispatchingConfig config, ILogger<AgentMatchingEngine> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Find the best agent for a work item.
    /// </summary>
    public DispatchResultModel RecommendAgent(
        IWorkItem workItem,
        IEnumerable<Agent> agents)
    {
        var result = new DispatchResultModel { WorkItemId = workItem.Id };

        try
        {
            // Filter to available agents with required competency
            var candidates = agents
                .Where(a => a.IsAvailable && a.Competencies.Contains(workItem.WorkType))
                .ToList();

            if (!candidates.Any())
            {
                // Fallback: try any available agent (skill mismatch but better than nothing)
                candidates = agents.Where(a => a.IsAvailable).ToList();

                if (!candidates.Any())
                {
                    result.ErrorMessage = "No available agents for assignment";
                    _logger.LogWarning(
                        "Agent-Matching: No available agents for {WorkItemId} ({WorkType})",
                        workItem.Id, workItem.WorkType);
                    return result;
                }
            }

            // Score each candidate
            var scoredAgents = candidates
                .Select(agent => new
                {
                    Agent = agent,
                    Score = ScoreAgent(agent, workItem)
                })
                .OrderByDescending(x => x.Score)
                .ToList();

            var bestMatch = scoredAgents.First();
            result.RecommendedAgentId = bestMatch.Agent.Id;
            result.MatchScore = bestMatch.Score;
            result.Rationale = GenerateRationale(bestMatch.Agent, workItem, bestMatch.Score);

            // Build score breakdown
            var breakdown = CalculateBreakdown(bestMatch.Agent, workItem);
            result.ScoreBreakdown = breakdown;

            _logger.LogInformation(
                "Agent-Matching: WorkItem {WorkItemId} assigned to {AgentId} ({AgentName}) with score {Score:F0}% (Confident: {Confident})",
                workItem.Id, bestMatch.Agent.Id, bestMatch.Agent.Name, bestMatch.Score, result.IsConfident);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent-Matching: Failed to match agent for {WorkItemId}", workItem.Id);
            result.ErrorMessage = $"Agent matching failed: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Calculate score for a candidate agent (0-100).
    /// </summary>
    private decimal ScoreAgent(Agent agent, IWorkItem workItem)
    {
        var skillMatch = CalculateSkillMatch(agent, workItem);
        var workloadBalance = CalculateWorkloadBalance(agent);
        var affinity = CalculateAffinity(agent, workItem);
        var availability = CalculateAvailability(agent);

        var score = (skillMatch * _config.SkillMatchWeight) +
                   (workloadBalance * _config.WorkloadBalanceWeight) +
                   (affinity * _config.AffinityWeight) +
                   (availability * _config.AvailabilityWeight);

        return Math.Min(score, 100m);
    }

    /// <summary>
    /// Calculate skill match score (0-100).
    /// 100 if agent has required competency, 20 otherwise.
    /// </summary>
    private decimal CalculateSkillMatch(Agent agent, IWorkItem workItem)
    {
        return agent.Competencies.Contains(workItem.WorkType) ? 100m : 20m;
    }

    /// <summary>
    /// Calculate workload balance score (0-100).
    /// Higher score for agents with lower utilization.
    /// </summary>
    private decimal CalculateWorkloadBalance(Agent agent)
    {
        var utilization = agent.UtilizationRatio;

        if (utilization < _config.OptimalUtilizationThreshold)
        {
            // Below optimal: 100 - (utilization * 50)
            return 100m - (utilization * 50m);
        }
        else
        {
            // Above optimal: 50 - (utilization * 50) = more aggressive penalty
            return 50m - (utilization * 50m);
        }
    }

    /// <summary>
    /// Calculate affinity score (0-100).
    /// Currently returns 0 (extensible for ML/historical data).
    /// </summary>
    private decimal CalculateAffinity(Agent agent, IWorkItem workItem)
    {
        // TODO: Integrate with ML model or historical assignment success data
        // For now: placeholder for future enhancement
        return 0m;
    }

    /// <summary>
    /// Calculate availability score (0-100).
    /// 100 if agent has capacity, 0 if overloaded.
    /// </summary>
    private decimal CalculateAvailability(Agent agent)
    {
        return agent.IsAvailable ? 100m : 0m;
    }

    /// <summary>
    /// Generate human-readable rationale for the assignment.
    /// </summary>
    private string GenerateRationale(Agent agent, IWorkItem workItem, decimal score)
    {
        var skillMatch = CalculateSkillMatch(agent, workItem);
        var workload = CalculateWorkloadBalance(agent);
        var confidence = score >= _config.ConfidenceThreshold ? "confident" : "tentative";

        var reasons = new List<string>();

        if (skillMatch == 100m)
            reasons.Add($"expert in {workItem.WorkType}");
        else
            reasons.Add("no prior experience");

        if (workload >= 80m)
            reasons.Add("light workload");
        else if (workload >= 50m)
            reasons.Add("moderate workload");
        else
            reasons.Add("high workload");

        return $"{agent.Name} ({score:F0}%) - {confidence} assignment ({string.Join(", ", reasons)})";
    }

    /// <summary>
    /// Calculate detailed score breakdown for transparency.
    /// </summary>
    private Dictionary<string, decimal> CalculateBreakdown(Agent agent, IWorkItem workItem)
    {
        var skillMatch = CalculateSkillMatch(agent, workItem);
        var workloadBalance = CalculateWorkloadBalance(agent);
        var affinity = CalculateAffinity(agent, workItem);
        var availability = CalculateAvailability(agent);

        return new Dictionary<string, decimal>
        {
            { "SkillMatch", skillMatch },
            { "WorkloadBalance", workloadBalance },
            { "Affinity", affinity },
            { "Availability", availability },
            { "SkillMatchWeight", skillMatch * _config.SkillMatchWeight },
            { "WorkloadWeight", workloadBalance * _config.WorkloadBalanceWeight },
            { "AffinityWeight", affinity * _config.AffinityWeight },
            { "AvailabilityWeight", availability * _config.AvailabilityWeight }
        };
    }
}
