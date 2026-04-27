using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Engine.GERDA.Knowledge;

/// <summary>
/// A similarity-based strategy for suggesting KB articles.
/// Uses simple keyword matching and tag affinity.
/// </summary>
public class SimilarityKnowledgeStrategy : IKnowledgeStrategy
{
    public string Name => "Similarity";

    public Task<List<KnowledgeSuggestion>> FindRelatedArticlesAsync(Ticket ticket, IEnumerable<KnowledgeBaseArticle> articles, int maxSuggestions)
    {
        var suggestions = new List<KnowledgeSuggestion>();

        // Combine title and description for analysis
        var ticketContent = $"{(ticket.Title ?? "")} {ticket.Description}".ToLowerInvariant();
        var ticketTags = (ticket.GerdaTags ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                                .Select(t => t.ToLowerInvariant())
                                                .ToList();

        foreach (var article in articles)
        {
            double score = 0;
            var reasons = new List<string>();

            // 1. Tag matching (High weight)
            var articleTags = (article.Tags ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                                 .Select(t => t.ToLowerInvariant())
                                                 .ToList();

            var matchingTags = ticketTags.Intersect(articleTags).ToList();
            if (matchingTags.Any())
            {
                score += matchingTags.Count * 0.3;
                reasons.Add($"Matches tags: {string.Join(", ", matchingTags)}");
            }

            // 2. Title keyword matching
            var titleWords = (article.Title ?? "").ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var titleMatchCount = titleWords.Count(w => w.Length > 3 && ticketContent.Contains(w));
            if (titleMatchCount > 0)
            {
                score += titleMatchCount * 0.1;
                reasons.Add("Title keywords found in ticket");
            }

            // 3. Content simple overlap (Lower weight)
            // Simplified check: if article content mentions the main ticket tags or keywords
            var contentWords = (article.Content ?? "").ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var contentMatchCount = ticketTags.Count(t => article.Content?.ToLowerInvariant().Contains(t) == true);
            if (contentMatchCount > 0)
            {
                score += contentMatchCount * 0.05;
                reasons.Add("Content relates to ticket tags");
            }

            if (score > 0)
            {
                suggestions.Add(new KnowledgeSuggestion
                {
                    Article = article,
                    RelevanceScore = Math.Min(score, 1.0), // Cap at 1.0 for now
                    MatchingReason = reasons.Any() ? reasons.First() : "Topic similarity"
                });
            }
        }

        return Task.FromResult(suggestions
            .OrderByDescending(s => s.RelevanceScore)
            .Take(maxSuggestions)
            .ToList());
    }
}
