using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Engine.GERDA.Knowledge;

/// <summary>
/// Interface for GERDA Knowledge Service - providing AI-suggested KB articles
/// </summary>
public interface IKnowledgeService
{
    /// <summary>
    /// Gets suggested knowledge base articles for a given ticket
    /// </summary>
    /// <param name="ticket">The ticket to analyze</param>
    /// <param name="maxSuggestions">Max number of suggestions to return</param>
    /// <returns>List of suggested KB articles with relevance scores</returns>
    Task<List<KnowledgeSuggestion>> GetSuggestedArticlesAsync(Ticket ticket, int maxSuggestions = 3);

    /// <summary>
    /// Check if knowledge base suggestions are enabled
    /// </summary>
    bool IsEnabled { get; }
}

public class KnowledgeSuggestion
{
    public KnowledgeBaseArticle Article { get; set; } = null!;
    public double RelevanceScore { get; set; }
    public string MatchingReason { get; set; } = string.Empty;
}
