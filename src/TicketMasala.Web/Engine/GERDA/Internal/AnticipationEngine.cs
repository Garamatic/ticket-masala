using TicketMasala.Web.Engine.GERDA.Anticipation;
using TicketMasala.Web.Engine.GERDA.Models;

namespace TicketMasala.Web.Engine.GERDA;

/// <summary>
/// Internal engine implementation for Anticipation stage (A).
/// </summary>
internal sealed class AnticipationEngine : IAnticipationEngine
{
    private readonly IAnticipationService _anticipationService;
    private readonly GerdaConfig _config;

    public AnticipationEngine(
        IAnticipationService anticipationService,
        GerdaConfig config)
    {
        _anticipationService = anticipationService;
        _config = config;
    }

    public bool IsEnabled => _config.GerdaAI.Anticipation.IsEnabled && _anticipationService.IsEnabled;

    public async Task<CapacityRisk?> CheckCapacityRiskAsync()
    {
        var risk = await _anticipationService.CheckCapacityRiskAsync();
        if (risk == null)
            return null;

        return new CapacityRisk(
            risk.AlertMessage,
            risk.RiskPercentage,
            risk.ForecastedInflow,
            (int)risk.AvailableCapacity
        );
    }
}
