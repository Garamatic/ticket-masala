using TicketMasala.Domain.Entities;
using TicketMasala.Web.Engine.GERDA.Dispatching.Algorithms;
using TicketMasala.Web.Engine.GERDA.Dispatching.Models;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Engine.GERDA.Dispatching;

[Obsolete]
public class AgentMatchingEngineDispatchingStrategy : IDispatchingStrategy
{
    public string Name => _engine.GetType().Name;

    private readonly AgentMatchingEngine _engine;
    private readonly IAffinityScorer? _affinityScorer;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AgentMatchingEngineDispatchingStrategy> _logger;

    public AgentMatchingEngineDispatchingStrategy(
        AgentMatchingEngine engine,
        IAffinityScorer? affinityScorer,
        IUserRepository userRepository,
        ILogger<AgentMatchingEngineDispatchingStrategy> logger)
    {
        _engine = engine;
        _affinityScorer = affinityScorer;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<List<DispatchResult>> GetRecommendedAgentsAsync(Ticket ticket, int count)
    {
        try
        {
            var employees = await _userRepository.GetAllEmployeesAsync();
            if (!employees.Any()) return new List<DispatchResult>();

            var customer = !string.IsNullOrEmpty(ticket.CustomerId)
                ? await _userRepository.GetCustomerByIdAsync(ticket.CustomerId)
                : null;

            var results = new List<DispatchResult>();
            foreach (var employee in employees)
            {
                double affinity = 0.5;
                string explanation = string.Empty;

                if (_affinityScorer != null && _affinityScorer.IsReady)
                {
                    affinity = _affinityScorer.CalculateAffinity(employee, ticket, customer);
                    explanation = _affinityScorer.GetAffinityExplanation(affinity, employee, ticket);
                }

                var result = new DispatchResult(employee.Id, affinity);
                result.Explanation = explanation;
                result.Reasons.Add(affinity > 0.7 ? "High affinity" : affinity > 0.4 ? "Moderate affinity" : "Low affinity");
                results.Add(result);
            }

            return results
                .OrderByDescending(r => r.Score)
                .Take(count)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRecommendedAgents failed for ticket {TicketGuid}", ticket.Guid);
            return new List<DispatchResult>();
        }
    }

    public Task RetrainModelAsync() => Task.CompletedTask;

    public DateTime? LastTrained => null;
}
