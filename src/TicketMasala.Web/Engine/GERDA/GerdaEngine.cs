using System.Diagnostics;
using System.Diagnostics.Metrics;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Engine.GERDA.Anticipation;
using TicketMasala.Web.Engine.GERDA.Dispatching;
using TicketMasala.Web.Engine.GERDA.Estimating;
using TicketMasala.Web.Engine.GERDA.Grouping;
using TicketMasala.Web.Engine.GERDA.Knowledge;
using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Engine.GERDA.Ranking;
using TicketMasala.Web.Repositories;

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
    private readonly ITicketRepository _ticketRepository;

    // Stage engines (internal — callers don't know these exist)
    private readonly IGroupingEngine _grouping;
    private readonly IEstimatingEngine _estimating;
    private readonly IRankingEngine _ranking;
    private readonly IDispatchingEngine _dispatching;
    private readonly IKnowledgeEngine _knowledge;
    private readonly IAnticipationEngine? _anticipation;

    public GerdaEngine(
        GerdaConfig config,
        ILogger<GerdaEngine> logger,
        ITicketRepository ticketRepository,
        IGroupingEngine grouping,
        IEstimatingEngine estimating,
        IRankingEngine ranking,
        IDispatchingEngine dispatching,
        IKnowledgeEngine knowledge,
        IAnticipationEngine? anticipation = null)
    {
        _config = config;
        _logger = logger;
        _ticketRepository = ticketRepository;
        _grouping = grouping;
        _estimating = estimating;
        _ranking = ranking;
        _dispatching = dispatching;
        _knowledge = knowledge;
        _anticipation = anticipation;
    }

    /// <inheritdoc />
    public bool IsActive => _config.GerdaAI.IsEnabled &&
        (_grouping.IsEnabled || _estimating.IsEnabled || _ranking.IsEnabled ||
         _dispatching.IsEnabled || _knowledge.IsEnabled || (_anticipation?.IsEnabled ?? false));

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

        try
        {
            // Execute stages sequentially, collecting results
            // G - Grouping
            Guid? parentGuid = null;
            using (var stageActivity = ActivitySource.StartActivity("GERDA.Stage.Grouping", ActivityKind.Internal))
            {
                var stageStopwatch = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    parentGuid = _grouping.IsEnabled
                        ? await _grouping.CheckAndGroupAsync(ticketGuid)
                        : null;

                    StageExecutionsCounter.Add(1, new KeyValuePair<string, object?>("stage", "grouping"));
                    stageActivity?.SetTag("stage.enabled", _grouping.IsEnabled);
                    stageActivity?.SetTag("stage.result", parentGuid.HasValue ? "grouped" : "not_grouped");
                }
                catch (Exception ex)
                {
                    StageFailuresCounter.Add(1, new KeyValuePair<string, object?>("stage", "grouping"));
                    stageActivity?.SetTag("error", true);
                    stageActivity?.SetTag("error.message", ex.Message);
                    throw;
                }
                finally
                {
                    stageStopwatch.Stop();
                    StageDurationHistogram.Record(stageStopwatch.ElapsedMilliseconds,
                        new KeyValuePair<string, object?>("stage", "grouping"));
                }
            }

            if (parentGuid.HasValue)
            {
                _logger.LogInformation("GERDA-G: Ticket {TicketGuid} grouped under parent {ParentGuid}",
                    ticketGuid, parentGuid.Value);
            }

            // E - Estimating
            double? effortPoints = null;
            using (var stageActivity = ActivitySource.StartActivity("GERDA.Stage.Estimating", ActivityKind.Internal))
            {
                var stageStopwatch = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    effortPoints = _estimating.IsEnabled
                        ? await _estimating.EstimateAsync(ticketGuid)
                        : null;

                    StageExecutionsCounter.Add(1, new KeyValuePair<string, object?>("stage", "estimating"));
                    stageActivity?.SetTag("stage.enabled", _estimating.IsEnabled);
                    stageActivity?.SetTag("stage.result", effortPoints?.ToString() ?? "disabled");
                }
                catch (Exception ex)
                {
                    StageFailuresCounter.Add(1, new KeyValuePair<string, object?>("stage", "estimating"));
                    stageActivity?.SetTag("error", true);
                    stageActivity?.SetTag("error.message", ex.Message);
                    throw;
                }
                finally
                {
                    stageStopwatch.Stop();
                    StageDurationHistogram.Record(stageStopwatch.ElapsedMilliseconds,
                        new KeyValuePair<string, object?>("stage", "estimating"));
                }
            }

            if (effortPoints.HasValue)
            {
                _logger.LogInformation("GERDA-E: Ticket {TicketGuid} estimated at {Points} effort points",
                    ticketGuid, effortPoints);
            }

            // R - Ranking
            double? priorityScore = null;
            using (var stageActivity = ActivitySource.StartActivity("GERDA.Stage.Ranking", ActivityKind.Internal))
            {
                var stageStopwatch = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    if (_ranking.IsEnabled)
                    {
                        priorityScore = await _ranking.CalculatePriorityAsync(ticketGuid);
                    }

                    StageExecutionsCounter.Add(1, new KeyValuePair<string, object?>("stage", "ranking"));
                    stageActivity?.SetTag("stage.enabled", _ranking.IsEnabled);
                    stageActivity?.SetTag("stage.result", priorityScore?.ToString("F2") ?? "disabled");
                }
                catch (Exception ex)
                {
                    StageFailuresCounter.Add(1, new KeyValuePair<string, object?>("stage", "ranking"));
                    stageActivity?.SetTag("error", true);
                    stageActivity?.SetTag("error.message", ex.Message);
                    throw;
                }
                finally
                {
                    stageStopwatch.Stop();
                    StageDurationHistogram.Record(stageStopwatch.ElapsedMilliseconds,
                        new KeyValuePair<string, object?>("stage", "ranking"));
                }
            }

            if (priorityScore.HasValue)
            {
                _logger.LogInformation("GERDA-R: Ticket {TicketGuid} priority score: {Score}",
                    ticketGuid, priorityScore.Value);
            }

            // D - Dispatching
            Guid? recommendedAgent = null;
            using (var stageActivity = ActivitySource.StartActivity("GERDA.Stage.Dispatching", ActivityKind.Internal))
            {
                var stageStopwatch = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    if (_dispatching.IsEnabled)
                    {
                        var agentId = await _dispatching.RecommendAgentAsync(ticketGuid);
                        if (!string.IsNullOrEmpty(agentId) && Guid.TryParse(agentId, out var agentGuid))
                        {
                            recommendedAgent = agentGuid;
                        }
                    }

                    StageExecutionsCounter.Add(1, new KeyValuePair<string, object?>("stage", "dispatching"));
                    stageActivity?.SetTag("stage.enabled", _dispatching.IsEnabled);
                    stageActivity?.SetTag("stage.result", recommendedAgent?.ToString() ?? "no_match");
                }
                catch (Exception ex)
                {
                    StageFailuresCounter.Add(1, new KeyValuePair<string, object?>("stage", "dispatching"));
                    stageActivity?.SetTag("error", true);
                    stageActivity?.SetTag("error.message", ex.Message);
                    throw;
                }
                finally
                {
                    stageStopwatch.Stop();
                    StageDurationHistogram.Record(stageStopwatch.ElapsedMilliseconds,
                        new KeyValuePair<string, object?>("stage", "dispatching"));
                }
            }

            if (recommendedAgent.HasValue)
            {
                _logger.LogInformation("GERDA-D: Recommended agent {AgentId} for ticket {TicketGuid}",
                    recommendedAgent.Value, ticketGuid);
            }

            // K - Knowledge
            List<Guid> suggestedArticles = new();
            using (var stageActivity = ActivitySource.StartActivity("GERDA.Stage.Knowledge", ActivityKind.Internal))
            {
                var stageStopwatch = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    if (_knowledge.IsEnabled)
                    {
                        var ticket = await _ticketRepository.GetByIdAsync(ticketGuid);
                        if (ticket != null)
                        {
                            var articles = await _knowledge.SuggestArticlesAsync(ticket);
                            suggestedArticles = articles.ToList();
                            _logger.LogInformation("GERDA-K: Found {Count} suggested articles for ticket {TicketGuid}",
                                suggestedArticles.Count, ticketGuid);
                        }
                    }

                    StageExecutionsCounter.Add(1, new KeyValuePair<string, object?>("stage", "knowledge"));
                    stageActivity?.SetTag("stage.enabled", _knowledge.IsEnabled);
                    stageActivity?.SetTag("stage.result", $"{suggestedArticles.Count}_articles");
                }
                catch (Exception ex)
                {
                    StageFailuresCounter.Add(1, new KeyValuePair<string, object?>("stage", "knowledge"));
                    stageActivity?.SetTag("error", true);
                    stageActivity?.SetTag("error.message", ex.Message);
                    throw;
                }
                finally
                {
                    stageStopwatch.Stop();
                    StageDurationHistogram.Record(stageStopwatch.ElapsedMilliseconds,
                        new KeyValuePair<string, object?>("stage", "knowledge"));
                }
            }

            stopwatch.Stop();
            PipelineDurationHistogram.Record(stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("ticket_guid", ticketGuid));
            TicketsProcessedCounter.Add(1,
                new KeyValuePair<string, object?>("result", "success"),
                new KeyValuePair<string, object?>("stages_executed", GetEnabledStagesTag()));

            _logger.LogInformation("GERDA: Completed processing ticket {TicketGuid} in {ElapsedMs}ms",
                ticketGuid, stopwatch.ElapsedMilliseconds);

            activity?.SetTag("result", "success");
            activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);

            return new GerdaOutcome(
                ticketGuid,
                WasGrouped: parentGuid.HasValue,
                EstimatedEffort: effortPoints,
                PriorityScore: priorityScore,
                SuggestedAgentId: recommendedAgent,
                RelatedArticles: suggestedArticles
            );
        }
        catch (Exception ex)
        {
            TicketsProcessedCounter.Add(1,
                new KeyValuePair<string, object?>("result", "failure"),
                new KeyValuePair<string, object?>("error_type", ex.GetType().Name));

            activity?.SetTag("error", true);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            _logger.LogError(ex, "GERDA: Error processing ticket {TicketGuid}", ticketGuid);
            throw;
        }
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

        // Determine which stages to run (distinct to prevent duplicates)
        var stagesToRun = selectedStages?.Length > 0
            ? selectedStages.Distinct().ToList()
            : GetEnabledStages();

        Guid? parentGuid = null;
        double? effortPoints = null;
        double? priorityScore = null;
        Guid? recommendedAgent = null;
        List<Guid> suggestedArticles = new();

        foreach (var stage in stagesToRun)
        {
            linkedToken.ThrowIfCancellationRequested();
            var stageStopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                switch (stage)
                {
                    case GerdaStage.Grouping when _grouping.IsEnabled:
                        parentGuid = await _grouping.CheckAndGroupAsync(ticketGuid);
                        break;

                    case GerdaStage.Estimating when _estimating.IsEnabled:
                        effortPoints = await _estimating.EstimateAsync(ticketGuid);
                        break;

                    case GerdaStage.Ranking when _ranking.IsEnabled:
                        priorityScore = await _ranking.CalculatePriorityAsync(ticketGuid);
                        break;

                    case GerdaStage.Dispatching when _dispatching.IsEnabled:
                        var agentId = await _dispatching.RecommendAgentAsync(ticketGuid);
                        if (!string.IsNullOrEmpty(agentId) && Guid.TryParse(agentId, out var agentGuid))
                        {
                            recommendedAgent = agentGuid;
                        }
                        break;

                    case GerdaStage.Knowledge when _knowledge.IsEnabled:
                        var ticket = await _ticketRepository.GetByIdAsync(ticketGuid);
                        if (ticket != null)
                        {
                            suggestedArticles = (await _knowledge.SuggestArticlesAsync(ticket)).ToList();
                        }
                        break;

                    case GerdaStage.Anticipation when _anticipation?.IsEnabled == true:
                        // Anticipation is batch-only, skip for single ticket
                        break;
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
                    WasExecuted: true,
                    stageStopwatch.Elapsed,
                    RawResult: GetStageResult(stage, parentGuid, effortPoints, priorityScore, recommendedAgent, suggestedArticles),
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
            WasGrouped: parentGuid.HasValue,
            EstimatedEffort: effortPoints,
            PriorityScore: priorityScore,
            SuggestedAgentId: recommendedAgent,
            RelatedArticles: suggestedArticles
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
        var stages = new List<GerdaStage>();
        if (_grouping.IsEnabled)
            stages.Add(GerdaStage.Grouping);
        if (_estimating.IsEnabled)
            stages.Add(GerdaStage.Estimating);
        if (_ranking.IsEnabled)
            stages.Add(GerdaStage.Ranking);
        if (_dispatching.IsEnabled)
            stages.Add(GerdaStage.Dispatching);
        if (_knowledge.IsEnabled)
            stages.Add(GerdaStage.Knowledge);
        return stages;
    }

    private string GetEnabledStagesTag()
    {
        var stages = new List<string>();
        if (_grouping.IsEnabled)
            stages.Add("G");
        if (_estimating.IsEnabled)
            stages.Add("E");
        if (_ranking.IsEnabled)
            stages.Add("R");
        if (_dispatching.IsEnabled)
            stages.Add("D");
        if (_knowledge.IsEnabled)
            stages.Add("K");
        if (_anticipation?.IsEnabled == true)
            stages.Add("A");
        return string.Join("", stages);
    }

    private static object? GetStageResult(
        GerdaStage stage,
        Guid? parentGuid,
        double? effortPoints,
        double? priorityScore,
        Guid? recommendedAgent,
        List<Guid> suggestedArticles)
    {
        return stage switch
        {
            GerdaStage.Grouping => parentGuid,
            GerdaStage.Estimating => effortPoints,
            GerdaStage.Ranking => priorityScore,
            GerdaStage.Dispatching => recommendedAgent,
            GerdaStage.Knowledge => suggestedArticles,
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
