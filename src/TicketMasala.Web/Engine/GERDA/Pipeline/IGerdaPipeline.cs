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
    /// <param name="ticketGuid">The ticket to process</param>
    /// <returns>PipelineResult with detailed execution status for each stage</returns>
    Task<PipelineResult> ExecuteAsync(Guid ticketGuid);

    /// <summary>
    /// Executes the pipeline with custom options.
    /// </summary>
    /// <param name="ticketGuid">The ticket to process</param>
    /// <param name="options">Pipeline execution options</param>
    /// <returns>PipelineResult with detailed execution status for each stage</returns>
    Task<PipelineResult> ExecuteAsync(Guid ticketGuid, PipelineOptions options);
}

/// <summary>
/// Default implementation of the GERDA pipeline.
/// Executes stages sequentially, supporting both "continue on error" and "fail fast" modes.
/// Replaces silent failure with explicit PipelineResult (Issue #8).
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

    /// <summary>
    /// Executes the pipeline with default options (ContinueOnError for backward compatibility).
    /// </summary>
    public Task<PipelineResult> ExecuteAsync(Guid ticketGuid)
    {
        return ExecuteAsync(ticketGuid, new PipelineOptions
        {
            ExecutionMode = PipelineExecutionMode.ContinueOnError,
            CaptureTiming = true
        });
    }

    /// <summary>
    /// Executes the pipeline with custom options.
    /// </summary>
    public async Task<PipelineResult> ExecuteAsync(Guid ticketGuid, PipelineOptions options)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var context = new GerdaPipelineContext();
        var result = new PipelineResult
        {
            TicketGuid = ticketGuid,
            Context = context,
            ExecutionMode = options.ExecutionMode
        };

        _logger.LogInformation(
            "GERDA Pipeline: Processing ticket {TicketGuid} through {StageCount} enabled stages (Mode: {ExecutionMode})",
            ticketGuid, _stages.Count, options.ExecutionMode);

        foreach (var stage in _stages)
        {
            var stageStopwatch = options.CaptureTiming ? System.Diagnostics.Stopwatch.StartNew() : null;

            try
            {
                _logger.LogDebug(
                    "GERDA Pipeline: Executing stage {StageName} for ticket {TicketGuid}",
                    stage.StageName, ticketGuid);

                await stage.ExecuteAsync(ticketGuid, context);

                stageStopwatch?.Stop();

                _logger.LogDebug(
                    "GERDA Pipeline: Completed stage {StageName} for ticket {TicketGuid} in {DurationMs}ms",
                    stage.StageName, ticketGuid, stageStopwatch?.ElapsedMilliseconds ?? 0);

                result.AddStageResult(new StageResult
                {
                    StageName = stage.StageName,
                    Status = StageStatus.Succeeded,
                    Duration = stageStopwatch?.Elapsed ?? TimeSpan.Zero
                });
            }
            catch (Exception ex)
            {
                stageStopwatch?.Stop();

                _logger.LogError(ex,
                    "GERDA Pipeline: Stage {StageName} failed for ticket {TicketGuid} after {DurationMs}ms",
                    stage.StageName, ticketGuid, stageStopwatch?.ElapsedMilliseconds ?? 0);

                result.AddStageResult(new StageResult
                {
                    StageName = stage.StageName,
                    Status = StageStatus.Failed,
                    Duration = stageStopwatch?.Elapsed ?? TimeSpan.Zero,
                    Error = new StageError
                    {
                        StageName = stage.StageName,
                        Message = ex.Message,
                        ExceptionDetails = ex.ToString()
                    }
                });

                // Fail fast mode: stop pipeline on first failure
                if (options.ExecutionMode == PipelineExecutionMode.FailFast)
                {
                    _logger.LogWarning(
                        "GERDA Pipeline: Aborting pipeline for ticket {TicketGuid} due to FailFast mode",
                        ticketGuid);
                    break;
                }

                // ContinueOnError mode: continue to next stage (legacy behavior)
            }
        }

        stopwatch.Stop();
        result = result with { Duration = stopwatch.Elapsed };

        // Log summary
        if (result.HasFailures)
        {
            _logger.LogWarning(
                "GERDA Pipeline: Completed with failures for ticket {TicketGuid}. {Summary}",
                ticketGuid, result.GetSummary());
        }
        else
        {
            _logger.LogInformation(
                "GERDA Pipeline: Successfully completed for ticket {TicketGuid}. {Summary}",
                ticketGuid, result.GetSummary());
        }

        return result;
    }
}
