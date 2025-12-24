using System.Threading.Tasks;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.Engine.GERDA.Estimating;

/// <summary>
/// No-op implementation of IEstimatingService for fallback scenarios.
/// </summary>
public class NoOpEstimatingService : IEstimatingService
{
    public bool IsEnabled => false;

    public Task<int> EstimateComplexityAsync(Guid ticketGuid)
    {
        return Task.FromResult(0);
    }

    public int GetComplexityByCategory(string category)
    {
        return 0;
    }
}
