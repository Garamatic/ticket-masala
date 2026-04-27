using TicketMasala.Web.Engine.GERDA.Estimating;

namespace TicketMasala.Web.Engine.GERDA.Pipeline.Stages;

/// <summary>
/// GERDA Stage: Estimating (E).
/// Calculates complexity/effort points for a ticket.
/// </summary>
public class EstimatingStage : IGerdaStage
{
    private readonly IEstimatingService _estimatingService;
    private readonly ILogger<EstimatingStage> _logger;

    public EstimatingStage(IEstimatingService estimatingService, ILogger<EstimatingStage> logger)
    {
        _estimatingService = estimatingService;
        _logger = logger;
    }

    public string StageName => "Estimating";
    public bool IsEnabled => true; // Always enabled

    public async Task ExecuteAsync(Guid ticketGuid, GerdaPipelineContext context)
    {
        var effortPoints = await _estimatingService.EstimateComplexityAsync(ticketGuid);
        context.EffortPoints = effortPoints;
        
        _logger.LogInformation(
            "GERDA-E: Ticket {TicketGuid} estimated at {Points} effort points",
            ticketGuid, effortPoints);
    }
}
