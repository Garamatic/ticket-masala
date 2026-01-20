using TicketMasala.Domain.Entities;
using TicketMasala.Web.Engine.GERDA.Strategies;

namespace TicketMasala.Web.Engine.GERDA.Knowledge;

/// <summary>
/// Strategy interface for knowledge recommendation algorithms
/// </summary>
public interface IKnowledgeStrategy : IStrategy<List<KnowledgeSuggestion>>
{
    /// <summary>
    /// Finds relevant KB articles for a ticket
    /// </summary>
    Task<List<KnowledgeSuggestion>> FindRelatedArticlesAsync(Ticket ticket, IEnumerable<KnowledgeBaseArticle> articles, int maxSuggestions);
}

