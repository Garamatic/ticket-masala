using TicketMasala.Web.Engine.GERDA.Dispatching;

namespace TicketMasala.Web.Engine.GERDA.Pipeline.Stages;

/// <summary>
/// GERDA Stage: Dispatching (D).
/// Recommends the best agent to handle a ticket using ML.
/// </summary>
public class DispatchingStage : IGerdaStage
{
    private readonly ITicketDispatcher? _dispatcher;
    private readonly ILogger<DispatchingStage> _logger;

    public DispatchingStage(ITicketDispatcher? dispatcher, ILogger<DispatchingStage> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public string StageName => "Dispatching";
    public bool IsEnabled => _dispatcher?.IsEnabled ?? false;

    public async Task ExecuteAsync(Guid ticketGuid, GerdaPipelineContext context)
    {
        if (_dispatcher == null || !_dispatcher.IsEnabled)
            return;

        var result = await _dispatcher.ExecuteAsync(new RecommendAgentsCommand(ticketGuid, 1));

        if (result.Success && result.Recommendations.Count > 0)
        {
            var topAgent = result.Recommendations[0];
            if (Guid.TryParse(topAgent.AgentId, out var agentGuid))
            {
                context.RecommendedAgentId = agentGuid;
                _logger.LogInformation(
                    "GERDA-D: Recommended agent {AgentId} (score {Score:F2}) for ticket {TicketGuid}",
                    topAgent.AgentId, topAgent.Score, ticketGuid);
            }
        }
    }
}
