using IT_Project2526.Models;
using IT_Project2526.Services.GERDA.Models;

namespace IT_Project2526.Services.GERDA.Ranking
{
    // Minimal seasonal priority strategy used as a fallback for tests
    public class SeasonalPriorityStrategy : IJobRankingStrategy
    {
        public string Name => "SeasonalPriority";

        public double CalculateScore(Ticket ticket, GerdaConfig config)
        {
            // Apply a simple seasonal multiplier based on month.
            // For demonstration, months in summer (6-8) increase priority for landscaping-like domains.
            var month = DateTime.UtcNow.Month;
            double seasonMultiplier = (month >= 6 && month <= 8) ? 1.5 : 1.0;

            // Base on a simple WSJF-like fallback using age and estimated effort
            var ageDays = (DateTime.UtcNow - ticket.CreationDate).TotalDays;
            var jobSize = ticket.EstimatedEffortPoints > 0 ? ticket.EstimatedEffortPoints : 5;

            // costOfDelay simplified: age * sla weight
            var costOfDelay = ageDays * (config?.GerdaAI?.Ranking?.SlaWeight ?? 1.0);

            return (costOfDelay / (double)jobSize) * seasonMultiplier;
        }
    }
}
