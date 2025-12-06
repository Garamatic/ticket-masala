using IT_Project2526.Models;
using IT_Project2526.Services.GERDA.Strategies;
using IT_Project2526.Services.GERDA.Models;

namespace IT_Project2526.Services.GERDA.Ranking
{
    public interface IJobRankingStrategy : IStrategy<double>
    {
        double CalculateScore(Ticket ticket, GerdaConfig config);
    }
}
