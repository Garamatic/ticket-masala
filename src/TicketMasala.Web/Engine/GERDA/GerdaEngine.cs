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

        _logger.LogInformation("GERDA: Processing ticket {TicketGuid}", ticketGuid);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Execute stages sequentially, collecting results
            // G - Grouping
            var parentGuid = _grouping.IsEnabled
                ? await _grouping.CheckAndGroupAsync(ticketGuid)
                : null;

            if (parentGuid.HasValue)
            {
                _logger.LogInformation("GERDA-G: Ticket {TicketGuid} grouped under parent {ParentGuid}",
                    ticketGuid, parentGuid.Value);
            }

            // E - Estimating
            var effortPoints = _estimating.IsEnabled
                ? await _estimating.EstimateAsync(ticketGuid)
                : null;

            if (effortPoints.HasValue)
            {
                _logger.LogInformation("GERDA-E: Ticket {TicketGuid} estimated at {Points} effort points",
                    ticketGuid, effortPoints.Value);
            }

            // R - Ranking
            double? priorityScore = null;
            if (_ranking.IsEnabled)
            {
                priorityScore = await _ranking.CalculatePriorityAsync(ticketGuid);
                _logger.LogInformation("GERDA-R: Ticket {TicketGuid} priority score: {Score}",
                    ticketGuid, priorityScore.Value);
            }

            // D - Dispatching
            Guid? recommendedAgent = null;
            if (_dispatching.IsEnabled)
            {
                var agentId = await _dispatching.RecommendAgentAsync(ticketGuid);
                if (!string.IsNullOrEmpty(agentId) && Guid.TryParse(agentId, out var agentGuid))
                {
                    recommendedAgent = agentGuid;
                    _logger.LogInformation("GERDA-D: Recommended agent {AgentId} for ticket {TicketGuid}",
                        agentId, ticketGuid);
                }
            }

            // K - Knowledge
            List<Guid> suggestedArticles = new();
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

            stopwatch.Stop();
            _logger.LogInformation("GERDA: Completed processing ticket {TicketGuid} in {ElapsedMs}ms",
                ticketGuid, stopwatch.ElapsedMilliseconds);

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

        var linkedToken = cts != null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token).Token
            : cancellationToken;

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
