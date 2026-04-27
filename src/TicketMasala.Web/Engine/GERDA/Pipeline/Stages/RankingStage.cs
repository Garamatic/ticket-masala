using TicketMasala.Web.Engine.GERDA.Ranking;

namespace TicketMasala.Web.Engine.GERDA.Pipeline.Stages;

/// <summary>
/// GERDA Stage: Ranking (R).
/// Calculates priority score using WSJF or other ranking algorithms.
/// </summary>
public class RankingStage : IGerdaStage
{
    private readonly IRankingService? _rankingService;
    private readonly ILogger<RankingStage> _logger;

    public RankingStage(IRankingService? rankingService, ILogger<RankingStage> logger)
    {
        _rankingService = rankingService;
        _logger = logger;
    }

    public string StageName => "Ranking";
    public bool IsEnabled => _rankingService?.IsEnabled ?? false;

    public async Task ExecuteAsync(Guid ticketGuid, GerdaPipelineContext context)
    {
        if (_rankingService == null)
            return;

        var priorityScore = await _rankingService.CalculatePriorityScoreAsync(ticketGuid);
        context.PriorityScore = priorityScore;

        _logger.LogInformation(
            "GERDA-R: Ticket {TicketGuid} priority score: {Score}",
            ticketGuid, priorityScore);
    }
}
