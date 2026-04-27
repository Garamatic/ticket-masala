using TicketMasala.Web.Engine.GERDA.Estimating;
using TicketMasala.Web.Engine.GERDA.Models;

namespace TicketMasala.Web.Engine.GERDA;

/// <summary>
/// Internal engine implementation for Estimating stage (E).
/// </summary>
internal sealed class EstimatingEngine : IEstimatingEngine
{
    private readonly IEstimatingService _estimatingService;
    private readonly GerdaConfig _config;

    public EstimatingEngine(
        IEstimatingService estimatingService,
        GerdaConfig config)
    {
        _estimatingService = estimatingService;
        _config = config;
    }

    public bool IsEnabled => _config.GerdaAI.ComplexityEstimation.IsEnabled;

    public async Task<double?> EstimateAsync(Guid ticketGuid)
    {
        var result = await _estimatingService.EstimateComplexityAsync(ticketGuid);
        return result;
    }
}

/// <summary>
/// No-op implementation when Estimating is disabled.
/// </summary>
internal sealed class NoOpEstimatingEngine : IEstimatingEngine
{
    public bool IsEnabled => false;

    public Task<double?> EstimateAsync(Guid ticketGuid)
    {
        return Task.FromResult<double?>(null);
    }
}
