// using TicketMasala.Web.Engine.GERDA.Dispatching.Models; // Not available

using TicketMasala.Web.Engine.GERDA.Dispatching.Models;

namespace TicketMasala.Web.Engine.Common;

/// <summary>
/// Adapter that wraps the standard Agent model for TicketMasala context.
/// Extends Agent with additional workload tracking properties if needed.
/// </summary>
public class TicketMasalaAgent : Agent
{
    /// <summary>
    /// Create a TicketMasalaAgent from a standard Agent.
    /// </summary>
    public static TicketMasalaAgent FromAgent(Agent agent)
    {
        var masalaAgent = new TicketMasalaAgent
        {
            Id = agent.Id,
            Name = agent.Name,
            Department = agent.Department,
            Competencies = agent.Competencies,
            CurrentCaseCount = agent.CurrentCaseCount,
            MaxCapacity = agent.MaxCapacity,
            SuccessRate = agent.SuccessRate,
            AverageResolutionTimeHours = agent.AverageResolutionTimeHours
        };
        return masalaAgent;
    }

    /// <summary>
    /// Create a standard Agent from a TicketMasalaAgent.
    /// </summary>
    // Method commented out: Agent type not available
    // public Agent ToAgent() => new()
    // {
    //     Id = this.Id,
    //     Name = this.Name,
    //     Department = this.Department,
    //     Competencies = this.Competencies,
    //     CurrentCaseCount = this.CurrentCaseCount,
    //     MaxCapacity = this.MaxCapacity,
    //     SuccessRate = this.SuccessRate,
    //     AverageResolutionTimeHours = this.AverageResolutionTimeHours
    // };

    /// <summary>Current number of assigned work items</summary>
    public int CurrentWorkload { get; set; }

    /// <summary>Maximum work items this agent can handle</summary>
    public int MaxCapacity { get; set; }

    /// <summary>Availability as percentage (0.0 to 1.0)</summary>
    public decimal AvailabilityPercentage { get; set; } = 1.0m;

    /// <summary>Historical success rate on similar work (0-100)</summary>
    public decimal? HistoricalSuccessRate { get; set; }

    /// <summary>Average resolution time in hours for this agent's domain</summary>
    public decimal? AverageResolutionTimeHours { get; set; }

    /// <summary>
    /// Get utilization ratio: CurrentWorkload / MaxCapacity (0.0 to 1.0+)
    /// </summary>
    public decimal GetUtilization() => MaxCapacity > 0 
        ? (decimal)CurrentWorkload / MaxCapacity 
        : 0m;

    /// <summary>
    /// Check if agent has capacity for more work
    /// </summary>
    public bool HasCapacity() => CurrentWorkload < MaxCapacity;

    /// <summary>
    /// Check if agent is available (has capacity AND is available percentage > 0)
    /// </summary>
    public bool IsAvailable() => HasCapacity() && AvailabilityPercentage > 0;
}
