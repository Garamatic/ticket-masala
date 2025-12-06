using TicketMasala.Web.Models;
using TicketMasala.Web.Engine.GERDA.Strategies;
using TicketMasala.Web.Engine.GERDA.Models;

namespace TicketMasala.Web.Engine.GERDA.Estimating;
    public interface IEstimatingStrategy : IStrategy<int>
    {
        int EstimateComplexity(Ticket ticket, GerdaConfig config);
}
