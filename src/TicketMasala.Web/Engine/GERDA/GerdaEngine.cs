using System.Diagnostics;
using System.Diagnostics.Metrics;
using TicketMasala.Web.Engine.GERDA.Models;

namespace TicketMasala.Web.Engine.GERDA;

/// <summary>
/// Deep module implementation of GERDA.
/// Hides 7 stage services and 15+ strategies behind a simple interface.
/// </summary>
internal sealed class GerdaEngine : IGerda
{
    // ═════════════════════════════════════════════════════════════════════════════
    // OpenTelemetry Tracing & Metrics
    // ═════════════════════════════════════════════════════════════════════════════
    private static readonly ActivitySource ActivitySource = new("TicketMasala.GERDA");
    private static readonly Meter Meter = new("TicketMasala.GERDA", "1.0.0");

    // Counters
    private static readonly Counter<long> TicketsProcessedCounter =
        Meter.CreateCounter<long>("gerda.tickets.processed", "tickets", "Total tickets processed by GERDA");
    private static readonly Counter<long> StageExecutionsCounter =
        Meter.CreateCounter<long>("gerda.stage.executions", "executions", "Total stage executions");
    private static readonly Counter<long> StageFailuresCounter =
        Meter.CreateCounter<long>("gerda.stage.failures", "failures", "Total stage execution failures");

    // Histograms
    private static readonly Histogram<double> PipelineDurationHistogram =
        Meter.CreateHistogram<double>("gerda.pipeline.duration_ms", "ms", "Pipeline execution duration");
    private static readonly Histogram<double> StageDurationHistogram =
        Meter.CreateHistogram<double>("gerda.stage.duration_ms", "ms", "Individual stage execution duration");

    private readonly GerdaConfig _config;
    private readonly ILogger<GerdaEngine> _logger;
    private readonly IGerdaStageProvider _stageProvider;

    public GerdaEngine(
        GerdaConfig config,
        ILogger<GerdaEngine> logger,
        IGerdaStageProvider stageProvider)
    {
        _config = config;
        _logger = logger;
        _stageProvider = stageProvider;
    }

    /// <inheritdoc />
    public bool IsActive => _config.GerdaAI.IsEnabled &&
        _stageProvider.GetStages().Any(stage => stage.IsEnabled);

    /// <inheritdoc />
    public async Task<GerdaOutcome> ProcessAsync(Guid ticketGuid)
    {
        if (!IsActive)
        {
            _logger.LogDebug("GERDA is disabled, skipping ticket processing for {TicketGuid}", ticketGuid);
            return GerdaOutcome.Disabled(ticketGuid);
        }

        using var activity = ActivitySource.StartActivity("GERDA.ProcessTicket", ActivityKind.Internal);
        activity?.SetTag("ticket.guid", ticketGuid);
        activity?.SetTag("gerda.enabled_stages", GetEnabledStagesTag());

        _logger.LogInformation("GERDA: Processing ticket {TicketGuid}", ticketGuid);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var stageFailures = new List<string>();
        var context = new GerdaExecutionContext();

        foreach (var stage in _stageProvider.GetStages())
        {
            using var stageActivity = ActivitySource.StartActivity($"GERDA.Stage.{stage.Stage}", ActivityKind.Internal);
            var stageStopwatch = Stopwatch.StartNew();
            var stageName = GetStageMetricName(stage.Stage);

            try
            {
                if (stage.IsEnabled)
                {
                    await stage.ExecuteAsync(ticketGuid, context);
                }

                StageExecutionsCounter.Add(1, new KeyValuePair<string, object?>("stage", stageName));
                stageActivity?.SetTag("stage.enabled", stage.IsEnabled);
                stageActivity?.SetTag("stage.result", GetStageResultTag(stage.Stage, context, stage.IsEnabled));
            }
            catch (Exception ex)
            {
                StageFailuresCounter.Add(1, new KeyValuePair<string, object?>("stage", stageName));
                stageActivity?.SetTag("error", true);
                stageActivity?.SetTag("error.message", ex.Message);

                _logger.LogWarning(ex,
                    "GERDA: Stage {Stage} failed for ticket {TicketGuid}; continuing with remaining stages",
                    stage.Stage, ticketGuid);

                stageFailures.Add(stageName);
            }
            finally
            {
                stageStopwatch.Stop();
                StageDurationHistogram.Record(stageStopwatch.ElapsedMilliseconds,
                    new KeyValuePair<string, object?>("stage", stageName));
            }
        }

        stopwatch.Stop();
        PipelineDurationHistogram.Record(stopwatch.ElapsedMilliseconds,
            new KeyValuePair<string, object?>("ticket_guid", ticketGuid));

        var hasPartialFailure = stageFailures.Count > 0;
        var overallResult = hasPartialFailure ? "partial" : "success";

        TicketsProcessedCounter.Add(1,
            new KeyValuePair<string, object?>("result", overallResult),
            new KeyValuePair<string, object?>("stages_executed", GetEnabledStagesTag()),
            new KeyValuePair<string, object?>("failed_stages", stageFailures.Count));

        _logger.LogInformation(
            "GERDA: Completed processing ticket {TicketGuid} in {ElapsedMs}ms ({Result}, {FailedCount} stage failures)",
            ticketGuid, stopwatch.ElapsedMilliseconds, overallResult, stageFailures.Count);

        activity?.SetTag("result", overallResult);
        activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);
        activity?.SetTag("failed_stages", stageFailures.Count);

        return new GerdaOutcome(
            ticketGuid,
            WasGrouped: context.ParentGuid.HasValue,
            EstimatedEffort: context.EffortPoints,
            PriorityScore: context.PriorityScore,
            SuggestedAgentId: context.RecommendedAgent,
            RelatedArticles: context.SuggestedArticles)
        {
            StageFailures = stageFailures
        };
    }

    /// <inheritdoc />
    public IGerdaAdvancedBuilder Configure()
    {
        return new GerdaAdvancedBuilder(this, _logger);
    }

    /// <summary>
    /// Internal advanced execution method called by the builder.
    /// </summary>
    internal async Task<GerdaDetailedResult> ExecuteAdvancedAsync(
        Guid ticketGuid,
        GerdaStage[]? selectedStages,
        Action<GerdaStageProgress>? progressCallback,
        TimeSpan? timeout,
        CancellationToken cancellationToken)
    {
        if (!IsActive)
        {
            return new GerdaDetailedResult
            {
                Outcome = GerdaOutcome.Disabled(ticketGuid),
                StageDetails = new Dictionary<GerdaStage, GerdaStageDetail>(),
                TotalDuration = TimeSpan.Zero,
                ExecutionLog = new List<GerdaStageProgress>()
            };
        }

        using var cts = timeout.HasValue
            ? new CancellationTokenSource(timeout.Value)
            : null;

        using var linkedCts = cts != null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token)
            : null;

        var linkedToken = linkedCts?.Token ?? cancellationToken;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var stageDetails = new Dictionary<GerdaStage, GerdaStageDetail>();
        var executionLog = new List<GerdaStageProgress>();
        var stageMap = _stageProvider.GetStages().ToDictionary(stage => stage.Stage);

        // Determine which stages to run (distinct to prevent duplicates)
        var stagesToRun = selectedStages?.Length > 0
            ? selectedStages.Distinct().ToList()
            : GetEnabledStages();

        var context = new GerdaExecutionContext();

        foreach (var stage in stagesToRun)
        {
            linkedToken.ThrowIfCancellationRequested();
            var stageStopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (!stageMap.TryGetValue(stage, out var stageImplementation))
                {
                    stageDetails[stage] = new GerdaStageDetail(
                        WasExecuted: false,
                        stageStopwatch.Elapsed,
                        RawResult: null,
                        Error: null
                    );
                    continue;
                }

                if (stageImplementation.IsEnabled)
                {
                    await stageImplementation.ExecuteAsync(ticketGuid, context);
                }

                stageStopwatch.Stop();
                var progress = new GerdaStageProgress(
                    stage,
                    ticketGuid,
                    stageStopwatch.Elapsed,
                    Succeeded: true,
                    ErrorMessage: null
                );
                executionLog.Add(progress);
                progressCallback?.Invoke(progress);

                stageDetails[stage] = new GerdaStageDetail(
                    WasExecuted: stageImplementation.IsEnabled,
                    stageStopwatch.Elapsed,
                    RawResult: GetStageResult(stage, context),
                    Error: null
                );
            }
            catch (Exception ex)
            {
                stageStopwatch.Stop();
                _logger.LogError(ex, "GERDA: Stage {Stage} failed for ticket {TicketGuid}", stage, ticketGuid);

                var progress = new GerdaStageProgress(
                    stage,
                    ticketGuid,
                    stageStopwatch.Elapsed,
                    Succeeded: false,
                    ErrorMessage: ex.Message
                );
                executionLog.Add(progress);
                progressCallback?.Invoke(progress);

                stageDetails[stage] = new GerdaStageDetail(
                    WasExecuted: true,
                    stageStopwatch.Elapsed,
                    RawResult: null,
                    Error: ex
                );

                // Continue with next stage (don't fail entire pipeline)
            }
        }

        stopwatch.Stop();

        var outcome = new GerdaOutcome(
            ticketGuid,
            WasGrouped: context.ParentGuid.HasValue,
            EstimatedEffort: context.EffortPoints,
            PriorityScore: context.PriorityScore,
            SuggestedAgentId: context.RecommendedAgent,
            RelatedArticles: context.SuggestedArticles
        );

        return new GerdaDetailedResult
        {
            Outcome = outcome,
            StageDetails = stageDetails,
            TotalDuration = stopwatch.Elapsed,
            ExecutionLog = executionLog
        };
    }

    private List<GerdaStage> GetEnabledStages()
    {
        return _stageProvider
            .GetStages()
            .Where(stage => stage.IsEnabled && stage.Stage != GerdaStage.Anticipation)
            .Select(stage => stage.Stage)
            .ToList();
    }

    private string GetEnabledStagesTag()
    {
        return string.Join("", _stageProvider
            .GetStages()
            .Where(stage => stage.IsEnabled)
            .Select(stage => stage.Stage switch
            {
                GerdaStage.Grouping => "G",
                GerdaStage.Estimating => "E",
                GerdaStage.Ranking => "R",
                GerdaStage.Dispatching => "D",
                GerdaStage.Knowledge => "K",
                GerdaStage.Anticipation => "A",
                _ => string.Empty
            }));
    }

    private static string GetStageMetricName(GerdaStage stage)
    {
        return stage switch
        {
            GerdaStage.Grouping => "grouping",
            GerdaStage.Estimating => "estimating",
            GerdaStage.Ranking => "ranking",
            GerdaStage.Dispatching => "dispatching",
            GerdaStage.Knowledge => "knowledge",
            GerdaStage.Anticipation => "anticipation",
            _ => "unknown"
        };
    }

    private static string GetStageResultTag(GerdaStage stage, GerdaExecutionContext context, bool isEnabled)
    {
        if (!isEnabled)
        {
            return "disabled";
        }

        return stage switch
        {
            GerdaStage.Grouping => context.ParentGuid.HasValue ? "grouped" : "not_grouped",
            GerdaStage.Estimating => context.EffortPoints?.ToString() ?? "disabled",
            GerdaStage.Ranking => context.PriorityScore?.ToString("F2") ?? "disabled",
            GerdaStage.Dispatching => context.RecommendedAgent?.ToString() ?? "no_match",
            GerdaStage.Knowledge => $"{context.SuggestedArticles.Count}_articles",
            GerdaStage.Anticipation => "batch_only",
            _ => "unknown"
        };
    }

    private static object? GetStageResult(GerdaStage stage, GerdaExecutionContext context)
    {
        return stage switch
        {
            GerdaStage.Grouping => context.ParentGuid,
            GerdaStage.Estimating => context.EffortPoints,
            GerdaStage.Ranking => context.PriorityScore,
            GerdaStage.Dispatching => context.RecommendedAgent,
            GerdaStage.Knowledge => context.SuggestedArticles,
            _ => null
        };
    }
}

/// <summary>
/// Fluent builder implementation for advanced GERDA configuration.
/// </summary>
internal sealed class GerdaAdvancedBuilder : IGerdaAdvancedBuilder
{
    private readonly GerdaEngine _engine;
    private readonly ILogger _logger;

    private GerdaStage[]? _selectedStages;
    private Action<GerdaStageProgress>? _progressCallback;
    private TimeSpan? _timeout;

    public GerdaAdvancedBuilder(GerdaEngine engine, ILogger logger)
    {
        _engine = engine;
        _logger = logger;
    }

    public IGerdaAdvancedBuilder Stages(params GerdaStage[] stages)
    {
        _selectedStages = stages;
        return this;
    }

    public IGerdaAdvancedBuilder OnProgress(Action<GerdaStageProgress> progress)
    {
        _progressCallback = progress;
        return this;
    }

    public IGerdaAdvancedBuilder WithTimeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }

    public Task<GerdaDetailedResult> ExecuteAsync(Guid ticketGuid, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GERDA Advanced: Executing configured pipeline for ticket {TicketGuid}", ticketGuid);
        return _engine.ExecuteAdvancedAsync(
            ticketGuid,
            _selectedStages,
            _progressCallback,
            _timeout,
            cancellationToken);
    }
}
