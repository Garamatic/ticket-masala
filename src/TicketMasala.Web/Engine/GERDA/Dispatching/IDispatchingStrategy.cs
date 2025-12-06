using TicketMasala.Web.Models;
using TicketMasala.Web.Services.GERDA.Strategies;

namespace TicketMasala.Web.Engine.GERDA.Dispatching;
    public interface IDispatchingStrategy : IStrategy<List<(string AgentId, double Score)>>
    {
        Task<List<(string AgentId, double Score)>> GetRecommendedAgentsAsync(Ticket ticket, int count);
        Task RetrainModelAsync();
}
