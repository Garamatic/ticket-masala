namespace TicketMasala.Web.Engine.GERDA.Sentiment;

public interface ISentimentAnalyzer
{
    (double UrgencyScore, string SentimentLabel) Analyze(string subject, string body);
}
