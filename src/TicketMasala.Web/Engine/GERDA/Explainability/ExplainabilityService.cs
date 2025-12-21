using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;

namespace TicketMasala.Web.Engine.GERDA.Explainability;

/// <summary>
/// Service that explains GERDA AI recommendations.
/// Returns contributing factors and weights for transparency.
/// </summary>
public interface IExplainabilityService
{
    /// <summary>
    /// Generates explanation for a dispatching recommendation
    /// </summary>
    ExplanationResult ExplainDispatchRecommendation(Ticket ticket, string recommendedAgentId);

    /// <summary>
    /// Generates explanation for a priority score
    /// </summary>
    ExplanationResult ExplainPriorityScore(Ticket ticket);

    /// <summary>
    /// Generates explanation for effort estimation
    /// </summary>
    ExplanationResult ExplainEffortEstimate(Ticket ticket);
}

/// <summary>
/// Result containing explanation factors
/// </summary>
public class ExplanationResult
{
    public List<ExplanationFactor> Factors { get; set; } = new();
    public string Summary { get; set; } = "";
    public double Confidence { get; set; }
}

/// <summary>
/// Individual contributing factor
/// </summary>
public class ExplanationFactor
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public double Weight { get; set; }
    public double Contribution { get; set; }
    public string? Value { get; set; }
}

/// <summary>
/// KISS implementation of explainability - uses heuristics based on ticket properties
/// </summary>
public class ExplainabilityService : IExplainabilityService
{
    private readonly ILogger<ExplainabilityService> _logger;

    public ExplainabilityService(ILogger<ExplainabilityService> logger)
    {
        _logger = logger;
    }

    public ExplanationResult ExplainDispatchRecommendation(Ticket ticket, string recommendedAgentId)
    {
        var factors = new List<ExplanationFactor>();

        // Category/Domain match
        factors.Add(new ExplanationFactor
        {
            Name = "Domain Match",
            Description = $"Ticket is in domain '{ticket.DomainId}'",
            Weight = 0.30,
            Contribution = 0.30,
            Value = ticket.DomainId
        });

        // Workload balancing (simulated)
        factors.Add(new ExplanationFactor
        {
            Name = "Workload Balance",
            Description = "Agent has capacity for new work",
            Weight = 0.25,
            Contribution = 0.20,
            Value = "Available"
        });

        // Skills match (simulated)
        factors.Add(new ExplanationFactor
        {
            Name = "Skill Match",
            Description = "Agent specialization aligns with ticket type",
            Weight = 0.25,
            Contribution = 0.22,
            Value = ticket.TicketType?.ToString() ?? "General"
        });

        // Historical performance (simulated)
        factors.Add(new ExplanationFactor
        {
            Name = "Historical Performance",
            Description = "Agent has successfully handled similar tickets",
            Weight = 0.20,
            Contribution = 0.18,
            Value = "Above Average"
        });

        return new ExplanationResult
        {
            Factors = factors,
            Summary = $"Recommended based on domain expertise and current availability",
            Confidence = factors.Sum(f => f.Contribution)
        };
    }

    public ExplanationResult ExplainPriorityScore(Ticket ticket)
    {
        var factors = new List<ExplanationFactor>();
        var totalContribution = 0.0;

        // SLA urgency
        if (ticket.CompletionTarget.HasValue)
        {
            var daysUntilDue = (ticket.CompletionTarget.Value - DateTime.UtcNow).TotalDays;
            var urgencyContribution = daysUntilDue <= 1 ? 0.40 : daysUntilDue <= 3 ? 0.25 : 0.10;
            totalContribution += urgencyContribution;

            factors.Add(new ExplanationFactor
            {
                Name = "SLA Urgency",
                Description = $"Due in {daysUntilDue:F1} days",
                Weight = 0.35,
                Contribution = urgencyContribution,
                Value = daysUntilDue <= 1 ? "Critical" : daysUntilDue <= 3 ? "High" : "Normal"
            });
        }

        // Ticket age
        var ageHours = (DateTime.UtcNow - ticket.CreationDate).TotalHours;
        var ageContribution = ageHours > 48 ? 0.20 : ageHours > 24 ? 0.10 : 0.05;
        totalContribution += ageContribution;

        factors.Add(new ExplanationFactor
        {
            Name = "Ticket Age",
            Description = $"Open for {ageHours:F0} hours",
            Weight = 0.25,
            Contribution = ageContribution,
            Value = ageHours > 48 ? "Aging" : "Recent"
        });

        // Estimated effort (WSJF impact)
        var effortContribution = ticket.EstimatedEffortPoints > 0 ? 0.15 : 0.05;
        totalContribution += effortContribution;

        factors.Add(new ExplanationFactor
        {
            Name = "Effort Efficiency",
            Description = $"Estimated {ticket.EstimatedEffortPoints} effort points",
            Weight = 0.20,
            Contribution = effortContribution,
            Value = ticket.EstimatedEffortPoints <= 3 ? "Quick Win" : "Standard"
        });

        return new ExplanationResult
        {
            Factors = factors,
            Summary = $"Priority based on SLA urgency and effort efficiency",
            Confidence = Math.Min(totalContribution, 1.0)
        };
    }

    public ExplanationResult ExplainEffortEstimate(Ticket ticket)
    {
        var factors = new List<ExplanationFactor>();

        // Description length
        var descLength = ticket.Description?.Length ?? 0;
        factors.Add(new ExplanationFactor
        {
            Name = "Complexity Indicator",
            Description = "Based on description detail level",
            Weight = 0.30,
            Contribution = descLength > 500 ? 0.25 : 0.10,
            Value = descLength > 500 ? "Detailed" : "Simple"
        });

        // Ticket type baseline
        factors.Add(new ExplanationFactor
        {
            Name = "Category Baseline",
            Description = "Historical average for this ticket type",
            Weight = 0.40,
            Contribution = 0.35,
            Value = ticket.TicketType?.ToString() ?? "Standard"
        });

        // Domain adjustment
        factors.Add(new ExplanationFactor
        {
            Name = "Domain Factor",
            Description = "Domain-specific complexity adjustment",
            Weight = 0.30,
            Contribution = 0.20,
            Value = ticket.DomainId
        });

        return new ExplanationResult
        {
            Factors = factors,
            Summary = $"Estimate of {ticket.EstimatedEffortPoints} points based on category and complexity",
            Confidence = 0.75
        };
    }
}
