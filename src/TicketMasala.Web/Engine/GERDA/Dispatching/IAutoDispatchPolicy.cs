using TicketMasala.Web.Engine.GERDA.Models;

namespace TicketMasala.Web.Engine.GERDA.Dispatching;

public interface IAutoDispatchPolicy
{
    bool ShouldAutoDispatch(DispatchResult? bestMatch, out double minScore);
}

public sealed class ScoreThresholdAutoDispatchPolicy : IAutoDispatchPolicy
{
    private readonly GerdaConfig _config;

    public ScoreThresholdAutoDispatchPolicy(GerdaConfig config)
    {
        _config = config;
    }

    public bool ShouldAutoDispatch(DispatchResult? bestMatch, out double minScore)
    {
        minScore = _config.GerdaAI.Dispatching.AutoDispatchMinScore;
        return bestMatch != null && bestMatch.Score >= minScore;
    }
}
