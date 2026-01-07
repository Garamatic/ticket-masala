using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Engine.GERDA.Strategies;

namespace TicketMasala.Web.Engine.GERDA.Dispatching;

public interface IDispatchingStrategy : IStrategy<List<DispatchResult>>
{
    Task<List<DispatchResult>> GetRecommendedAgentsAsync(Ticket ticket, int count);
    Task RetrainModelAsync();
    DateTime? LastTrained { get; }
}
