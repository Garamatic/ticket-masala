using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Engine.GERDA.Strategies;

namespace TicketMasala.Web.Engine.GERDA.Estimating;

public interface IEstimatingStrategy : IStrategy<int>
{
    int EstimateComplexity(Ticket ticket, GerdaConfig config);
}
