namespace TicketMasala.Web.Engine.GERDA.Pipeline;

/// <summary>
/// Represents a single stage in the GERDA processing pipeline.
/// Each stage (Grouping, Estimating, Ranking, etc.) implements this interface.
/// </summary>
public interface IGerdaStage
{
    /// <summary>
    /// Gets the name of this stage (e.g., "Grouping", "Estimating").
    /// </summary>
    string StageName { get; }

    /// <summary>
    /// Gets whether this stage is enabled in the current configuration.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Executes this stage for the given ticket.
    /// </summary>
    /// <param name="ticketGuid">The ticket to process</param>
    /// <param name="context">Shared context for passing data between stages</param>
    Task ExecuteAsync(Guid ticketGuid, GerdaPipelineContext context);
}

/// <summary>
/// Context object passed between pipeline stages.
/// Allows stages to share data and results.
/// </summary>
public class GerdaPipelineContext
{
    public Dictionary<string, object> Data { get; } = new();
    
    public Guid? ParentTicketGuid { get; set; }
    public double? EffortPoints { get; set; }
    public double? PriorityScore { get; set; }
    public Guid? RecommendedAgentId { get; set; }
    public List<Guid> SuggestedArticles { get; set; } = new();
    
    /// <summary>
    /// Stores arbitrary data for custom stages.
    /// </summary>
    public void Set(string key, object value) => Data[key] = value;
    
    /// <summary>
    /// Retrieves arbitrary data stored by previous stages.
    /// </summary>
    public T? Get<T>(string key) => Data.TryGetValue(key, out var value) ? (T)value : default;
}
