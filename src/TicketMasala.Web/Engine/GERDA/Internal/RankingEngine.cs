using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Engine.GERDA.Ranking;

namespace TicketMasala.Web.Engine.GERDA;

/// <summary>
/// Internal engine implementation for Ranking stage (R).
/// </summary>
internal sealed class RankingEngine : IRankingEngine
{
    private readonly IRankingService _rankingService;
    private readonly GerdaConfig _config;

    public RankingEngine(
        IRankingService rankingService,
        GerdaConfig config)
    {
        _rankingService = rankingService;
        _config = config;
    }

    public bool IsEnabled => _config.GerdaAI.Ranking.IsEnabled && _rankingService.IsEnabled;

    public async Task<double?> CalculatePriorityAsync(Guid ticketGuid)
    {
        var result = await _rankingService.CalculatePriorityScoreAsync(ticketGuid);
        return result;
    }
}

/// <summary>
/// No-op implementation when Ranking is disabled.
/// </summary>
internal sealed class NoOpRankingEngine : IRankingEngine
{
    public bool IsEnabled => false;

    public Task<double?> CalculatePriorityAsync(Guid ticketGuid)
    {
        return Task.FromResult<double?>(null);
    }
}
