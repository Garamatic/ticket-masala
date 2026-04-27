using TicketMasala.Web.Engine.GERDA.Dispatching;
using TicketMasala.Web.Engine.GERDA.Models;

namespace TicketMasala.Web.Engine.GERDA;

/// <summary>
/// Internal engine implementation for Dispatching stage (D).
/// </summary>
internal sealed class DispatchingEngine : IDispatchingEngine
{
    private readonly IDispatchingService _dispatchingService;
    private readonly GerdaConfig _config;

    public DispatchingEngine(
        IDispatchingService dispatchingService,
        GerdaConfig config)
    {
        _dispatchingService = dispatchingService;
        _config = config;
    }

    public bool IsEnabled => _config.GerdaAI.Dispatching.IsEnabled && _dispatchingService.IsEnabled;

    public Task<string?> RecommendAgentAsync(Guid ticketGuid)
    {
        return _dispatchingService.GetRecommendedAgentAsync(ticketGuid);
    }
}

/// <summary>
/// No-op implementation when Dispatching is disabled.
/// </summary>
internal sealed class NoOpDispatchingEngine : IDispatchingEngine
{
    public bool IsEnabled => false;

    public Task<string?> RecommendAgentAsync(Guid ticketGuid)
    {
        return Task.FromResult<string?>(null);
    }
}
