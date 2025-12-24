namespace TicketMasala.Web.Engine.GERDA.Sentiment;

public static class SimpleSentimentAnalyzer
{
    private static readonly string[] UrgentKeywords = new[] { "urgent", "asap", "broken", "critical", "down", "fail", "emergency", "immediately" };
    private static readonly string[] NegativeKeywords = new[] { "disappointed", "poor", "bad", "slow", "error", "bug", "crash" };

    public static (double UrgencyScore, string SentimentLabel) Analyze(string subject, string body)
    {
        var text = (subject + " " + body).ToLowerInvariant();
        double score = 1.0; // Base score (Normal)

        // Urgency check
        int urgencyCount = UrgentKeywords.Count(k => text.Contains(k));
        if (urgencyCount > 0)
        {
            score += 2.0 + (urgencyCount * 0.5); // Boost significantly
        }

        // Negative sentiment check (increases priority slightly as "Risk")
        int negativeCount = NegativeKeywords.Count(k => text.Contains(k));
        if (negativeCount > 0)
        {
            score += 0.5 + (negativeCount * 0.2);
        }

        // Cap score at 5.0
        score = Math.Min(score, 5.0);

        string label = score switch
        {
            >= 4.0 => "Critical",
            >= 3.0 => "High",
            >= 2.0 => "Medium",
            _ => "Normal"
        };

        return (score, label);
    }
}
