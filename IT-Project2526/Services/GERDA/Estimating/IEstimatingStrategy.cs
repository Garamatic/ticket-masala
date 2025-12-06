using IT_Project2526.Models;
using IT_Project2526.Services.GERDA.Strategies;
using IT_Project2526.Services.GERDA.Models;

namespace IT_Project2526.Services.GERDA.Estimating
{
    public interface IEstimatingStrategy : IStrategy<int>
    {
        int EstimateComplexity(Ticket ticket, GerdaConfig config);
    }
}
