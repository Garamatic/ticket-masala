using TicketMasala.Web.Engine.GERDA.Dispatching;
using TicketMasala.Web.Engine.GERDA.Knowledge;
using TicketMasala.Web.Engine.GERDA.Ranking;

namespace TicketMasala.Web.Engine.GERDA;

/// <summary>
/// Null object implementation for IRankingService.
/// Used when ranking functionality is disabled to avoid null checks.
/// </summary>
public class NullRankingService : IRankingService
{
    public bool IsEnabled => false;

    public Task<double> CalculatePriorityScoreAsync(Guid ticketGuid)
    {
        return Task.FromResult(0.0);
    }

    public Task RecalculateAllPrioritiesAsync()
    {
        return Task.CompletedTask;
    }

    public Task<List<Guid>> GetPrioritizedTicketGuidsAsync(Guid? projectGuid = null)
    {
        return Task.FromResult(new List<Guid>());
    }
}

/// <summary>
/// Null object implementation for IDispatchingService.
/// Used when dispatching functionality is disabled to avoid null checks.
/// </summary>
public class NullDispatchingService : IDispatchingService
{
    public bool IsEnabled => false;

    public Task<AgentRecommendation?> GetRecommendedAgentAsync(Guid ticketGuid)
    {
        return Task.FromResult<AgentRecommendation?>(null);
    }

    public Task<List<AgentRecommendation>> GetTopRecommendedAgentsAsync(Guid ticketGuid, int count)
    {
        return Task.FromResult(new List<AgentRecommendation>());
    }

    public Task<List<AgentRecommendation>> GetRecommendedAgentsByTeamAsync(Guid ticketGuid, string teamCode, int count)
    {
        return Task.FromResult(new List<AgentRecommendation>());
    }

    public Task<DispatchingAnalysis> AnalyzeDispatchingAsync(Guid ticketGuid, string agentId)
    {
        return Task.FromResult(new DispatchingAnalysis
        {
            TicketGuid = ticketGuid,
            AgentId = agentId,
            Score = 0,
            Recommendation = "Auto-dispatching is disabled"
        });
    }

    public Task AutoDispatchAsync(Guid ticketGuid, string agentId)
    {
        return Task.CompletedTask;
    }

    public Task<CapacityAnalysis> GetCapacityAnalysisAsync(string agentId)
    {
        return Task.FromResult(new CapacityAnalysis
        {
            AgentId = agentId,
            CurrentWorkload = 0,
            MaxCapacity = 0,
            AvailableCapacity = 0
        });
    }
}

/// <summary>
/// Null object implementation for IKnowledgeService.
/// Used when knowledge base functionality is disabled to avoid null checks.
/// </summary>
public class NullKnowledgeService : IKnowledgeService
{
    public Task<List<KnowledgeArticleMatch>> GetSuggestedArticlesAsync(TicketMasala.Domain.Entities.Ticket ticket)
    {
        return Task.FromResult(new List<KnowledgeArticleMatch>());
    }

    public Task<List<KnowledgeArticleMatch>> SearchArticlesAsync(string query, int maxResults = 5)
    {
        return Task.FromResult(new List<KnowledgeArticleMatch>());
    }

    public Task<KnowledgeArticle?> GetArticleByIdAsync(Guid articleId)
    {
        return Task.FromResult<KnowledgeArticle?>(null);
    }
}
