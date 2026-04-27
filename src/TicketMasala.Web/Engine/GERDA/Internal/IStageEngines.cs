using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Engine.GERDA;

/// <summary>
/// Internal engine interface for Grouping stage (G).
/// Hidden from callers — only IGerda sees these.
/// </summary>
internal interface IGroupingEngine
{
    bool IsEnabled { get; }
    Task<Guid?> CheckAndGroupAsync(Guid ticketGuid);
}

/// <summary>
/// Internal engine interface for Estimating stage (E).
/// </summary>
internal interface IEstimatingEngine
{
    bool IsEnabled { get; }
    Task<double?> EstimateAsync(Guid ticketGuid);
}

/// <summary>
/// Internal engine interface for Ranking stage (R).
/// </summary>
internal interface IRankingEngine
{
    bool IsEnabled { get; }
    Task<double?> CalculatePriorityAsync(Guid ticketGuid);
}

/// <summary>
/// Internal engine interface for Dispatching stage (D).
/// </summary>
internal interface IDispatchingEngine
{
    bool IsEnabled { get; }
    Task<string?> RecommendAgentAsync(Guid ticketGuid);
}

/// <summary>
/// Internal engine interface for Knowledge stage (K).
/// </summary>
internal interface IKnowledgeEngine
{
    bool IsEnabled { get; }
    Task<IEnumerable<Guid>> SuggestArticlesAsync(Ticket ticket);
}

/// <summary>
/// Internal engine interface for Anticipation stage (A).
/// </summary>
internal interface IAnticipationEngine
{
    bool IsEnabled { get; }
    Task<CapacityRisk?> CheckCapacityRiskAsync();
}

/// <summary>
/// Capacity risk alert from anticipation engine.
/// </summary>
internal sealed record CapacityRisk(
    string AlertMessage,
    double RiskPercentage,
    int ForecastedTickets,
    int AvailableCapacity
);
