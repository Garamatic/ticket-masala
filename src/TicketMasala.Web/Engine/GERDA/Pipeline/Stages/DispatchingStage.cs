using TicketMasala.Web.Engine.GERDA.Dispatching;

namespace TicketMasala.Web.Engine.GERDA.Pipeline.Stages;

/// <summary>
/// GERDA Stage: Dispatching (D).
/// Recommends the best agent to handle a ticket using ML.
/// </summary>
public class DispatchingStage : IGerdaStage
{
    private readonly IDispatchingService? _dispatchingService;
    private readonly ILogger<DispatchingStage> _logger;

    public DispatchingStage(IDispatchingService? dispatchingService, ILogger<DispatchingStage> logger)
    {
        _dispatchingService = dispatchingService;
        _logger = logger;
    }

    public string StageName => "Dispatching";
    public bool IsEnabled => _dispatchingService?.IsEnabled ?? false;

    public async Task ExecuteAsync(Guid ticketGuid, GerdaPipelineContext context)
    {
        if (_dispatchingService == null)
            return;

        var recommendedAgent = await _dispatchingService.GetRecommendedAgentAsync(ticketGuid);
        
        if (!string.IsNullOrEmpty(recommendedAgent) && Guid.TryParse(recommendedAgent, out var agentGuid))
        {
            context.RecommendedAgentId = agentGuid;
            _logger.LogInformation(
                "GERDA-D: Recommended agent {AgentId} for ticket {TicketGuid}",
                recommendedAgent, ticketGuid);
        }
    }
}
