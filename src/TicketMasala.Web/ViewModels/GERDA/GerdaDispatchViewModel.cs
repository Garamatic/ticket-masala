using TicketMasala.Web.Models;

namespace TicketMasala.Web.ViewModels.GERDA;
    /// <summary>
    /// ViewModel for GERDA Dispatching Dashboard
    /// Shows backlog of unassigned tickets with AI recommendations
    /// </summary>
    public class GerdaDispatchViewModel
    {
        public List<TicketDispatchInfo> UnassignedTickets { get; set; } = new();
        public List<AgentInfo> AvailableAgents { get; set; } = new();
        public DispatchStatistics Statistics { get; set; } = new();
        public List<ProjectOption> Projects { get; set; } = new();
    }

    /// <summary>
    /// Ticket with GERDA AI recommendations
    /// </summary>
    public class TicketDispatchInfo
    {
        public Guid Guid { get; set; }
        public string Description { get; set; } = string.Empty;
        public Status TicketStatus { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? CompletionTarget { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
        
        // GERDA AI Fields
        public int EstimatedEffortPoints { get; set; }
        public double PriorityScore { get; set; }
        public string? GerdaTags { get; set; }
        
        // Project recommendation
        public Guid? RecommendedProjectGuid { get; set; }
        public string? RecommendedProjectName { get; set; }
        public Guid? CurrentProjectGuid { get; set; }
        public string? CurrentProjectName { get; set; }
        
        // Agent recommendations (top 3)
        public List<AgentRecommendation> RecommendedAgents { get; set; } = new();
        
        // Time in backlog
        public TimeSpan TimeInBacklog => DateTime.UtcNow - CreationDate;
        public string TimeInBacklogDisplay => FormatTimeInBacklog(TimeInBacklog);
        
        private static string FormatTimeInBacklog(TimeSpan time)
        {
            if (time.TotalHours < 1) return $"{(int)time.TotalMinutes}m";
            if (time.TotalDays < 1) return $"{(int)time.TotalHours}h";
            return $"{(int)time.TotalDays}d";
        }
    }

    /// <summary>
    /// Agent recommendation with score and explanation
    /// </summary>
    public class AgentRecommendation
    {
        public string AgentId { get; set; } = string.Empty;
        public string AgentName { get; set; } = string.Empty;
        public string Team { get; set; } = string.Empty;
        public double Score { get; set; }
        public int CurrentWorkload { get; set; }
        public int MaxCapacity { get; set; }
        public string? Specializations { get; set; }
        public string? Language { get; set; }
        public string? Region { get; set; }
        
        public string WorkloadDisplay => $"{CurrentWorkload}/{MaxCapacity}";
        public int WorkloadPercentage => MaxCapacity > 0 ? (int)((CurrentWorkload / (double)MaxCapacity) * 100) : 0;
        public string ScoreDisplay => $"{Score:F2}";
    }

    /// <summary>
    /// Agent information for dashboard
    /// </summary>
    public class AgentInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Team { get; set; } = string.Empty;
        public int CurrentWorkload { get; set; }
        public int MaxCapacity { get; set; }
        public int CurrentEffortPoints { get; set; }
        public int MaxCapacityPoints { get; set; }
        public string? Language { get; set; }
        public string? Region { get; set; }
        
        public bool IsAvailable => CurrentWorkload < MaxCapacity;
        public int AvailableSlots => Math.Max(0, MaxCapacity - CurrentWorkload);
        public int WorkloadPercentage => MaxCapacity > 0 ? (int)((CurrentWorkload / (double)MaxCapacity) * 100) : 0;
    }

    /// <summary>
    /// Statistics for dispatch dashboard
    /// </summary>
    public class DispatchStatistics
    {
        public int TotalUnassignedTickets { get; set; }
        public int TicketsWithProjectRecommendation { get; set; }
        public int TicketsWithAgentRecommendation { get; set; }
        public int TotalAvailableAgents { get; set; }
        public int OverloadedAgents { get; set; }
        public double AverageTicketAge { get; set; }
        public int HighPriorityTickets { get; set; }
        public int TicketsOlderThan24Hours { get; set; }
    }

    /// <summary>
    /// Project option for assignment
    /// </summary>
    public class ProjectOption
    {
        public Guid Guid { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int CurrentTicketCount { get; set; }
        public Status Status { get; set; }
    }

    /// <summary>
    /// Request for batch assignment
    /// </summary>
    public class BatchAssignRequest
    {
        public List<Guid> TicketGuids { get; set; } = new();
        public bool UseGerdaRecommendations { get; set; } = true;
        public string? ForceAgentId { get; set; }
        public Guid? ForceProjectGuid { get; set; }
    }

    /// <summary>
    /// Result of batch assignment
    /// </summary>
    public class BatchAssignResult
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<TicketAssignmentDetail> Assignments { get; set; } = new();
    }

    /// <summary>
    /// Detail of a single ticket assignment
    /// </summary>
    public class TicketAssignmentDetail
    {
        public Guid TicketGuid { get; set; }
        public string? AssignedAgentName { get; set; }
        public string? AssignedProjectName { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
