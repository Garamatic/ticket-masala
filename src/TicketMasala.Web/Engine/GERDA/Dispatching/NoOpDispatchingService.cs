using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TicketMasala.Web.Engine.GERDA.Dispatching
{
    public class NoOpDispatchingService : IDispatchingService
    {
        public bool IsEnabled => false;
        public Task<string?> GetRecommendedAgentAsync(Guid ticketGuid) => Task.FromResult<string?>(null);
        public Task<List<DispatchResult>> GetTopRecommendedAgentsAsync(Guid ticketGuid, int count = 3) => Task.FromResult(new List<DispatchResult>());
        public Task<bool> AutoDispatchTicketAsync(Guid ticketGuid) => Task.FromResult(false);
        public Task RetrainModelAsync() => Task.CompletedTask;
        public Task<string?> GetRecommendedProjectManagerAsync(Guid ticketGuid) => Task.FromResult<string?>(null);
        public DateTime? LastModelTrainingTime => null;
    }
}
