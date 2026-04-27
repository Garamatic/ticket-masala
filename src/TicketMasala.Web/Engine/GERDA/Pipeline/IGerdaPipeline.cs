namespace TicketMasala.Web.Engine.GERDA.Pipeline;

/// <summary>
/// Orchestrates the execution of GERDA stages in a pipeline pattern.
/// This replaces the god-object GerdaService with a cleaner, more extensible design.
/// </summary>
public interface IGerdaPipeline
{
    /// <summary>
    /// Executes all enabled stages in the pipeline for a single ticket.
    /// </summary>
    Task<GerdaPipelineContext> ExecuteAsync(Guid ticketGuid);
}

/// <summary>
/// Default implementation of the GERDA pipeline.
/// Executes stages sequentially, skipping disabled stages.
/// </summary>
public class ConfigurableGerdaPipeline : IGerdaPipeline
{
    private readonly List<IGerdaStage> _stages;
    private readonly ILogger<ConfigurableGerdaPipeline> _logger;

    public ConfigurableGerdaPipeline(
        IEnumerable<IGerdaStage> stages,
        ILogger<ConfigurableGerdaPipeline> logger)
    {
        _stages = stages.Where(s => s.IsEnabled).ToList();
        _logger = logger;
    }

    public async Task<GerdaPipelineContext> ExecuteAsync(Guid ticketGuid)
    {
        var context = new GerdaPipelineContext();

        _logger.LogInformation(
            "GERDA Pipeline: Processing ticket {TicketGuid} through {StageCount} enabled stages",
            ticketGuid, _stages.Count);

        foreach (var stage in _stages)
        {
            try
            {
                _logger.LogDebug(
                    "GERDA Pipeline: Executing stage {StageName} for ticket {TicketGuid}",
                    stage.StageName, ticketGuid);

                await stage.ExecuteAsync(ticketGuid, context);

                _logger.LogDebug(
                    "GERDA Pipeline: Completed stage {StageName} for ticket {TicketGuid}",
                    stage.StageName, ticketGuid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "GERDA Pipeline: Stage {StageName} failed for ticket {TicketGuid}. Continuing with next stage.",
                    stage.StageName, ticketGuid);

                // Continue with next stage instead of failing entire pipeline
            }
        }

        _logger.LogInformation(
            "GERDA Pipeline: Completed processing ticket {TicketGuid}",
            ticketGuid);

        return context;
    }
}
