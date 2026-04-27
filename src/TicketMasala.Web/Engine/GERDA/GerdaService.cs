using TicketMasala.Domain.Common;
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
/// Main GERDA orchestrator - coordinates all GERDA services
/// GERDA = GovTech Extended Resource Dispatch &amp; Anticipation
/// </summary>
public class GerdaService : IGerdaService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly GerdaConfig _config;
    private readonly ILogger<GerdaService> _logger;
    private readonly IGroupingService _groupingService;
    private readonly IEstimatingService _estimatingService;
    private readonly IRankingService _rankingService;
    private readonly IDispatchingService _dispatchingService;
    private readonly IKnowledgeService _knowledgeService;
    private readonly IAnticipationService? _anticipationService;

    public GerdaService(
        ITicketRepository ticketRepository,
        GerdaConfig config,
        ILogger<GerdaService> logger,
        IGroupingService groupingService,
        IEstimatingService estimatingService,
        IRankingService? rankingService = null,
        IDispatchingService? dispatchingService = null,
        IKnowledgeService? knowledgeService = null,
        IAnticipationService? anticipationService = null)
    {
        _ticketRepository = ticketRepository;
        _config = config;
        _logger = logger;
        _groupingService = groupingService;
        _estimatingService = estimatingService;
        // Use Null Object pattern to avoid null checks throughout the code
        _rankingService = rankingService ?? new NullRankingService();
        _dispatchingService = dispatchingService ?? new NullDispatchingService();
        _knowledgeService = knowledgeService ?? new NullKnowledgeService();
        _anticipationService = anticipationService;
    }

    public bool IsEnabled => _config.GerdaAI.IsEnabled;

    public async Task ProcessTicketAsync(Guid ticketGuid)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("GERDA is disabled, skipping ticket processing");
            return;
        }

        _logger.LogInformation("GERDA: Processing ticket {TicketGuid}", ticketGuid);

        try
        {
            // G - Grouping: Check for spam/clustering
            var parentGuid = await _groupingService.CheckAndGroupTicketAsync(ticketGuid);
            if (parentGuid.HasValue)
            {
                _logger.LogInformation("GERDA-G: Ticket {TicketGuid} grouped under parent {ParentGuid}", ticketGuid, parentGuid);
            }

            // E - Estimating: Calculate complexity
            var effortPoints = await _estimatingService.EstimateComplexityAsync(ticketGuid);
            _logger.LogInformation("GERDA-E: Ticket {TicketGuid} estimated at {Points} effort points", ticketGuid, effortPoints);

            // R - Ranking: Calculate priority score (if service is enabled)
            if (_rankingService.IsEnabled)
            {
                var priorityScore = await _rankingService.CalculatePriorityScoreAsync(ticketGuid);
                _logger.LogInformation("GERDA-R: Ticket {TicketGuid} priority score: {Score}", ticketGuid, priorityScore);
            }

            // D - Dispatching: Recommend agent (if service is enabled)
            if (_dispatchingService.IsEnabled)
            {
                var recommendedAgent = await _dispatchingService.GetRecommendedAgentAsync(ticketGuid);
                if (recommendedAgent != null)
                {
                    _logger.LogInformation("GERDA-D: Recommended agent {AgentId} for ticket {TicketGuid}", recommendedAgent, ticketGuid);
                }
            }

            // K - Knowledge: Suggest KB articles
            var ticket = await _ticketRepository.GetByIdAsync(ticketGuid);
            if (ticket != null)
            {
                var suggestions = await _knowledgeService.GetSuggestedArticlesAsync(ticket);
                _logger.LogInformation("GERDA-K: Found {Count} suggested articles for ticket {TicketGuid}", suggestions.Count, ticketGuid);
            }

            _logger.LogInformation("GERDA: Completed processing ticket {TicketGuid}", ticketGuid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GERDA: Error processing ticket {TicketGuid}", ticketGuid);
            throw;
        }
    }

    public async Task ProcessAllOpenTicketsAsync()
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("GERDA is disabled, skipping batch processing");
            return;
        }

        _logger.LogInformation("GERDA: Starting batch processing of all open tickets");

        // Use Repository to get all open/active tickets
        var activeTickets = await _ticketRepository.GetActiveTicketsAsync();
        var openTicketGuids = activeTickets.Select(t => t.Guid).ToList();

        _logger.LogInformation("GERDA: Found {Count} open tickets to process", openTicketGuids.Count);

        foreach (var ticketGuid in openTicketGuids)
        {
            try
            {
                await ProcessTicketAsync(ticketGuid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GERDA: Failed to process ticket {TicketGuid}, continuing with next", ticketGuid);
            }
        }

        // A - Anticipation: Check capacity forecast (if service is available)
        if (_anticipationService != null && _anticipationService.IsEnabled)
        {
            var risk = await _anticipationService.CheckCapacityRiskAsync();
            if (risk != null)
            {
                _logger.LogWarning(
                    "GERDA-A: Capacity risk detected! {Message} (Risk: {Percentage}%)",
                    risk.AlertMessage, risk.RiskPercentage);
            }
        }

        _logger.LogInformation("GERDA: Completed batch processing");
    }

}
