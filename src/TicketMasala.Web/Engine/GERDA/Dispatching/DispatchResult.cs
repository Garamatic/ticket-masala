namespace TicketMasala.Web.Engine.GERDA.Dispatching;

/// <summary>
/// Represents the result of a dispatching recommendation with explainability.
/// </summary>
public class DispatchResult
{
    public string AgentId { get; set; } = string.Empty;
    public double Score { get; set; }
    public List<string> Reasons { get; set; } = new();
    public string? Explanation { get; set; }

    public DispatchResult(string agentId, double score)
    {
        AgentId = agentId;
        Score = score;
    }
}
