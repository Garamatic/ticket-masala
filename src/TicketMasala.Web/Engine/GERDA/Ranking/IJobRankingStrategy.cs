using TicketMasala.Web.Models;
using TicketMasala.Web.Engine.GERDA.Strategies;
using TicketMasala.Web.Engine.GERDA.Models;

namespace TicketMasala.Web.Engine.GERDA.Ranking;

public interface IJobRankingStrategy : IStrategy<double>
{
    double CalculateScore(Ticket ticket, GerdaConfig config);
}
