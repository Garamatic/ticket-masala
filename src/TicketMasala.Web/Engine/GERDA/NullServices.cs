using TicketMasala.Domain.Entities;
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
/// Null object implementation for IKnowledgeService.
/// Used when knowledge base functionality is disabled to avoid null checks.
/// </summary>
public class NullKnowledgeService : IKnowledgeService
{
    public bool IsEnabled => false;

    public Task<List<KnowledgeSuggestion>> GetSuggestedArticlesAsync(Ticket ticket, int maxSuggestions = 3)
    {
        return Task.FromResult(new List<KnowledgeSuggestion>());
    }
}
