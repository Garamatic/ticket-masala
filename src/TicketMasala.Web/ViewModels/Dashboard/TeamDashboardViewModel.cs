using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;

namespace TicketMasala.Web.ViewModels.Dashboard;

/// <summary>
/// View model for the Manager Team Dashboard showing GERDA AI metrics
/// </summary>
public class TeamDashboardViewModel
{
    // Overall Ticket Metrics
    public int TotalActiveTickets { get; set; }
    public int UnassignedTickets { get; set; }
    public int AssignedTickets { get; set; }
    public int CompletedTickets { get; set; }
    public int OverdueTickets { get; set; }

    // GERDA AI Metrics
    public double AveragePriorityScore { get; set; }
    public double AverageComplexity { get; set; }
    public int AiAssignedCount { get; set; }
    public double AiAssignmentAcceptanceRate { get; set; } // % of AI recommendations accepted

    // SLA Metrics
    public int TicketsWithinSla { get; set; }
    public int TicketsBreachingSla { get; set; }
    public double SlaComplianceRate { get; set; } // % within SLA

    // Agent Workload Metrics
    public List<AgentWorkloadMetric> AgentWorkloads { get; set; } = new List<AgentWorkloadMetric>();

    // Priority Distribution (for histogram)
    public Dictionary<string, int> PriorityDistribution { get; set; } = new Dictionary<string, int>
        {
            { "Critical", 0 },
            { "High", 0 },
            { "Medium", 0 },
            { "Low", 0 }
        };

    // Complexity Distribution (for histogram)
    public Dictionary<string, int> ComplexityDistribution { get; set; } = new Dictionary<string, int>
        {
            { "Trivial", 0 },
            { "Simple", 0 },
            { "Medium", 0 },
            { "Complex", 0 },
            { "Very Complex", 0 }
        };

    // Top Tags (most common GERDA tags)
    public List<TagFrequency> TopTags { get; set; } = new List<TagFrequency>();

    // Recent Activity
    public List<RecentActivityItem> RecentActivity { get; set; } = new();

    // New Analytics
    public List<ForecastData> ForecastData { get; set; } = new();
    public List<AgentPerformanceMetric> AgentPerformance { get; set; } = new();
}

public class ForecastData
{
    public DateTime Date { get; set; }
    public int PredictedVolume { get; set; }
}

public class AgentPerformanceMetric
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public int ClosedTickets { get; set; }
}

public class AgentWorkloadMetric
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public int AssignedTicketCount { get; set; }
    public int CurrentWorkload { get; set; } // EstimatedEffortPoints sum
    public int MaxCapacity { get; set; }
    public double UtilizationPercentage { get; set; }
    public string UtilizationClass
    {
        get
        {
            if (UtilizationPercentage >= 90) return "danger";
            if (UtilizationPercentage >= 80) return "warning";
            return "success";
        }
    }
}

public class TagFrequency
{
    public string TagName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RecentActivityItem
{
    public DateTime Timestamp { get; set; }
    public string TicketGuid { get; set; } = string.Empty;
    public string TicketDescription { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty; // "Created", "Assigned", "Completed"
    public string AgentName { get; set; } = string.Empty;
    public string ActivityClass
    {
        get
        {
            return ActivityType switch
            {
                "Created" => "primary",
                "Assigned" => "info",
                "Completed" => "success",
                _ => "secondary"
            };
        }
    }
}
