using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Engine.GERDA.Strategies;

namespace TicketMasala.Web.Engine.GERDA.Ranking;

public interface IJobRankingStrategy : IStrategy<double>
{
    double CalculateScore(Ticket ticket, GerdaConfig config);
}
