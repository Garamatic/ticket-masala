namespace TicketMasala.Web.Engine.GERDA.Dispatching;

/// <summary>
/// No-op implementation when dispatching is disabled.
/// Returns empty results without executing any logic.
/// </summary>
public class NoOpDispatcher : ITicketDispatcher
{
    public Task<DispatcherResult> ExecuteAsync(IDispatchCommand command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(command switch
        {
            RecommendAgentsCommand => DispatcherResult.WithRecommendations(Array.Empty<AgentRecommendation>()),
            AutoDispatchCommand => DispatcherResult.Skipped("Dispatching is disabled"),
            RetrainCommand => DispatcherResult.Skipped("Dispatching is disabled"),
            _ => DispatcherResult.Fail($"Unknown command: {command.GetType().Name}")
        });
    }
}
