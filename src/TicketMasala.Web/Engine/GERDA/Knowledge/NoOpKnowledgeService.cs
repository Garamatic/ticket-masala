using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Engine.GERDA.Knowledge;

/// <summary>
/// No-Operation implementation of IKnowledgeService.
/// Used when GERDA AI configuration is missing or disabled.
/// </summary>
public class NoOpKnowledgeService : IKnowledgeService
{
    /// <summary>
    /// Always returns false - this is a no-op implementation
    /// </summary>
    public bool IsEnabled => false;

    public Task<List<KnowledgeSuggestion>> GetSuggestedArticlesAsync(Ticket ticket, int maxSuggestions = 3)
    {
        return Task.FromResult(new List<KnowledgeSuggestion>());
    }
}
