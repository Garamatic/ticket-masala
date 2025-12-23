using TicketMasala.Domain.Entities;

namespace TicketMasala.Domain.Services;

/// <summary>
/// Provides explanations for AI-driven decisions and recommendations.
/// </summary>
public interface IExplainabilityService
{
    /// <summary>
    /// Generates an explanation for why a specific agent was recommended for a ticket.
    /// </summary>
    /// <param name="ticket">The ticket being analyzed.</param>
    /// <param name="recommendedAgent">The agent recommended by GERDA.</param>
    /// <returns>A structured explanation object.</returns>
    Task<AiExplanation> ExplainAgentRecommendationAsync(Ticket ticket, string recommendedAgent);

    /// <summary>
    /// Generates an explanation for the assigned category/tags.
    /// </summary>
    Task<AiExplanation> ExplainClassificationAsync(Ticket ticket);
}

public class AiExplanation
{
    public string Summary { get; set; } = string.Empty;
    public List<ExplanationFactor> Factors { get; set; } = new();
    public double ConfidenceScore { get; set; }
}

public class ExplanationFactor
{
    public string Name { get; set; } = string.Empty;
    public double Weight { get; set; } // 0.0 to 1.0 impact
    public string Description { get; set; } = string.Empty;
}
