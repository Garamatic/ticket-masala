using TicketMasala.Domain.Entities;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Engine.GERDA;

/// <summary>
/// Provides the stage pipeline for GERDA execution.
/// Enables testing and composition without hard-coding stage dependencies in GerdaEngine.
/// </summary>
internal interface IGerdaStageProvider
{
    IReadOnlyList<IGerdaExecutionStage> GetStages();
}

/// <summary>
/// Internal stage contract used by GerdaEngine orchestration.
/// </summary>
internal interface IGerdaExecutionStage
{
    GerdaStage Stage { get; }
    bool IsEnabled { get; }
    Task ExecuteAsync(Guid ticketGuid, GerdaExecutionContext context);
}

/// <summary>
/// Shared state passed through the internal GERDA stage pipeline.
/// </summary>
internal sealed class GerdaExecutionContext
{
    public Guid? ParentGuid { get; set; }
    public double? EffortPoints { get; set; }
    public double? PriorityScore { get; set; }
    public Guid? RecommendedAgent { get; set; }
    public List<Guid> SuggestedArticles { get; } = new();
}

internal sealed class DefaultGerdaStageProvider : IGerdaStageProvider
{
    private static readonly IReadOnlyDictionary<GerdaStage, int> StageOrder =
        new Dictionary<GerdaStage, int>
        {
            [GerdaStage.Grouping] = 0,
            [GerdaStage.Estimating] = 1,
            [GerdaStage.Ranking] = 2,
            [GerdaStage.Dispatching] = 3,
            [GerdaStage.Knowledge] = 4,
            [GerdaStage.Anticipation] = 5
        };

    private readonly IReadOnlyList<IGerdaExecutionStage> _stages;

    public DefaultGerdaStageProvider(IEnumerable<IGerdaExecutionStage> stages)
    {
        _stages = stages
            .OrderBy(stage => StageOrder.TryGetValue(stage.Stage, out var order) ? order : int.MaxValue)
            .ToList();
    }

    public IReadOnlyList<IGerdaExecutionStage> GetStages() => _stages;
}

internal sealed class GroupingExecutionStage : IGerdaExecutionStage
{
    private readonly IGroupingEngine _grouping;

    public GroupingExecutionStage(IGroupingEngine grouping)
    {
        _grouping = grouping;
    }

    public GerdaStage Stage => GerdaStage.Grouping;
    public bool IsEnabled => _grouping.IsEnabled;

    public async Task ExecuteAsync(Guid ticketGuid, GerdaExecutionContext context)
    {
        context.ParentGuid = await _grouping.CheckAndGroupAsync(ticketGuid);
    }
}

internal sealed class EstimatingExecutionStage : IGerdaExecutionStage
{
    private readonly IEstimatingEngine _estimating;

    public EstimatingExecutionStage(IEstimatingEngine estimating)
    {
        _estimating = estimating;
    }

    public GerdaStage Stage => GerdaStage.Estimating;
    public bool IsEnabled => _estimating.IsEnabled;

    public async Task ExecuteAsync(Guid ticketGuid, GerdaExecutionContext context)
    {
        context.EffortPoints = await _estimating.EstimateAsync(ticketGuid);
    }
}

internal sealed class RankingExecutionStage : IGerdaExecutionStage
{
    private readonly IRankingEngine _ranking;

    public RankingExecutionStage(IRankingEngine ranking)
    {
        _ranking = ranking;
    }

    public GerdaStage Stage => GerdaStage.Ranking;
    public bool IsEnabled => _ranking.IsEnabled;

    public async Task ExecuteAsync(Guid ticketGuid, GerdaExecutionContext context)
    {
        context.PriorityScore = await _ranking.CalculatePriorityAsync(ticketGuid);
    }
}

internal sealed class DispatchingExecutionStage : IGerdaExecutionStage
{
    private readonly IDispatchingEngine _dispatching;

    public DispatchingExecutionStage(IDispatchingEngine dispatching)
    {
        _dispatching = dispatching;
    }

    public GerdaStage Stage => GerdaStage.Dispatching;
    public bool IsEnabled => _dispatching.IsEnabled;

    public async Task ExecuteAsync(Guid ticketGuid, GerdaExecutionContext context)
    {
        var agentId = await _dispatching.RecommendAgentAsync(ticketGuid);
        if (!string.IsNullOrEmpty(agentId) && Guid.TryParse(agentId, out var agentGuid))
        {
            context.RecommendedAgent = agentGuid;
        }
    }
}

internal sealed class KnowledgeExecutionStage : IGerdaExecutionStage
{
    private readonly IKnowledgeEngine _knowledge;
    private readonly ITicketRepository _ticketRepository;

    public KnowledgeExecutionStage(IKnowledgeEngine knowledge, ITicketRepository ticketRepository)
    {
        _knowledge = knowledge;
        _ticketRepository = ticketRepository;
    }

    public GerdaStage Stage => GerdaStage.Knowledge;
    public bool IsEnabled => _knowledge.IsEnabled;

    public async Task ExecuteAsync(Guid ticketGuid, GerdaExecutionContext context)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketGuid);
        if (ticket == null)
        {
            return;
        }

        context.SuggestedArticles.Clear();
        context.SuggestedArticles.AddRange(await _knowledge.SuggestArticlesAsync(ticket));
    }
}

internal sealed class AnticipationExecutionStage : IGerdaExecutionStage
{
    private readonly IAnticipationEngine _anticipation;

    public AnticipationExecutionStage(IAnticipationEngine anticipation)
    {
        _anticipation = anticipation;
    }

    public GerdaStage Stage => GerdaStage.Anticipation;
    public bool IsEnabled => _anticipation.IsEnabled;

    public Task ExecuteAsync(Guid ticketGuid, GerdaExecutionContext context)
    {
        // Anticipation is batch-oriented and does not mutate single-ticket outcome.
        return Task.CompletedTask;
    }
}
