using TicketMasala.Domain.Entities;
using TicketMasala.Web.Engine.GERDA.Dispatching;

namespace TicketMasala.Web.Engine.GERDA.Dispatching
{
    public class ZoneBasedDispatchingStrategy : IDispatchingStrategy
    {
        public string Name => "ZoneBased";

        public Task<List<DispatchResult>> GetRecommendedAgentsAsync(Ticket ticket, int count)
        {
            // Simple placeholder implementation
            // In a real scenario, this would check regions/zones
            var result = new List<DispatchResult>();
            // Since we don't have agents passed in, we return empty or dummy (though logic should likely query UserRepository)
            // For now, return empty to satisfy interface safely.
            return Task.FromResult(result);
        }

        public Task RetrainModelAsync()
        {
            return Task.CompletedTask;
        }

        public DateTime? LastTrained => null;
    }
}
