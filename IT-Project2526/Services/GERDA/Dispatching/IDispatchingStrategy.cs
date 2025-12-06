using IT_Project2526.Models;
using IT_Project2526.Services.GERDA.Strategies;

namespace IT_Project2526.Services.GERDA.Dispatching
{
    public interface IDispatchingStrategy : IStrategy<List<(string AgentId, double Score)>>
    {
        Task<List<(string AgentId, double Score)>> GetRecommendedAgentsAsync(Ticket ticket, int count);
        Task RetrainModelAsync();
    }
}
