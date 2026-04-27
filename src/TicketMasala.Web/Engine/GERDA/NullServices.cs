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

    public DateTime? LastModelTrainingTime => null;

    public Task<string?> GetRecommendedAgentAsync(Guid ticketGuid)
    {
        return Task.FromResult<string?>(null);
    }

    public Task<List<DispatchResult>> GetTopRecommendedAgentsAsync(Guid ticketGuid, int count = 3)
    {
        return Task.FromResult(new List<DispatchResult>());
    }

    public Task<bool> AutoDispatchTicketAsync(Guid ticketGuid)
    {
        return Task.FromResult(false);
    }

    public Task RetrainModelAsync()
    {
        return Task.CompletedTask;
    }

    public Task<string?> GetRecommendedProjectManagerAsync(Guid ticketGuid)
    {
        return Task.FromResult<string?>(null);
    }
}

/// <summary>
/// Null object implementation for IKnowledgeService.
/// Used when knowledge base functionality is disabled to avoid null checks.
/// </summary>
public class NullKnowledgeService : IKnowledgeService
{
    public Task<List<KnowledgeSuggestion>> GetSuggestedArticlesAsync(TicketMasala.Domain.Entities.Ticket ticket, int maxSuggestions = 3)
    {
        return Task.FromResult(new List<KnowledgeSuggestion>());
    }
}
