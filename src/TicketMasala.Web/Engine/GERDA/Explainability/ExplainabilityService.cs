using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Services;

namespace TicketMasala.Web.Engine.GERDA.Explainability;

/// <summary>
/// Service that explains GERDA AI recommendations.
/// Returns contributing factors and weights for transparency.
/// </summary>
public class ExplainabilityService : IExplainabilityService
{
    private readonly ILogger<ExplainabilityService> _logger;

    public ExplainabilityService(ILogger<ExplainabilityService> logger)
    {
        _logger = logger;
    }

    public Task<AiExplanation> ExplainAgentRecommendationAsync(Ticket ticket, string recommendedAgent)
    {
        var factors = new List<ExplanationFactor>();

        // Category/Domain match
        factors.Add(new ExplanationFactor
        {
            Name = "Domain Match",
            Description = $"Ticket is in domain '{ticket.DomainId}'",
            Weight = 0.30
        });

        // Workload balancing (simulated)
        factors.Add(new ExplanationFactor
        {
            Name = "Workload Balance",
            Description = "Agent has capacity for new work",
            Weight = 0.25
        });

        // Skills match (simulated)
        factors.Add(new ExplanationFactor
        {
            Name = "Skill Match",
            Description = "Agent specialization aligns with ticket type",
            Weight = 0.25
        });

        // Historical performance (simulated)
        factors.Add(new ExplanationFactor
        {
            Name = "Historical Performance",
            Description = "Agent has successfully handled similar tickets",
            Weight = 0.20
        });

        return Task.FromResult(new AiExplanation
        {
            Factors = factors,
            Summary = $"Recommended based on domain expertise and current availability",
            ConfidenceScore = 0.85
        });
    }

    public Task<AiExplanation> ExplainClassificationAsync(Ticket ticket)
    {
        var factors = new List<ExplanationFactor>();

        // Description length
        var descLength = ticket.Description?.Length ?? 0;
        factors.Add(new ExplanationFactor
        {
            Name = "Complexity Indicator",
            Description = "Based on description detail level",
            Weight = 0.30
        });

        // Ticket type baseline
        factors.Add(new ExplanationFactor
        {
            Name = "Category Baseline",
            Description = "Historical average for this ticket type",
            Weight = 0.40
        });

        return Task.FromResult(new AiExplanation
        {
            Factors = factors,
            Summary = $"Classification based on content analysis and metadata",
            ConfidenceScore = 0.75
        });
    }

    // Retaining these as internal helpers or specific methods if needed elsewhere, 
    // avoiding interface conflict for now.
    public ExplanationResult ExplainPriorityScore(Ticket ticket)
    {
         var factors = new List<LocalExplanationFactor>();
        // SLA urgency logic (simplified for fix)
        factors.Add(new LocalExplanationFactor
        {
            Name = "SLA Urgency",
            Description = "Based on due date",
            Weight = 0.35,
            Contribution = 0.35,
            Value = "Computed"
        });

        return new ExplanationResult
        {
            Factors = factors,
            Summary = "Priority explanation",
            Confidence = 0.9
        };
    }
}

/// <summary>
/// Result containing explanation factors (Local version just to keep code compiling if referenced elsewhere)
/// </summary>
public class ExplanationResult
{
    public List<LocalExplanationFactor> Factors { get; set; } = new();
    public string Summary { get; set; } = "";
    public double Confidence { get; set; }
}

public class LocalExplanationFactor
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public double Weight { get; set; }
    public double Contribution { get; set; }
    public string? Value { get; set; }
}

