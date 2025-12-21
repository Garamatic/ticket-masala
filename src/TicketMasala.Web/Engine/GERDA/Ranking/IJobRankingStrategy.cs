using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Engine.GERDA.Strategies;
using TicketMasala.Web.Engine.GERDA.Models;

namespace TicketMasala.Web.Engine.GERDA.Ranking;

public interface IJobRankingStrategy : IStrategy<double>
{
    double CalculateScore(Ticket ticket, GerdaConfig config);
}
