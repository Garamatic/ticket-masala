using IT_Project2526.Services.GERDA.Models;
using IT_Project2526.Services.GERDA.Grouping;
using IT_Project2526.Services.GERDA.Estimating;
using IT_Project2526.Services.GERDA.Ranking;
using IT_Project2526.Services.GERDA.Dispatching;
using IT_Project2526.Services.GERDA.Anticipation;
using IT_Project2526.Models;
using IT_Project2526.Repositories;

namespace IT_Project2526.Services.GERDA;

/// <summary>
/// Main GERDA orchestrator - coordinates all GERDA services
/// GERDA = GovTech Extended Resource Dispatch & Anticipation
/// </summary>
public class GerdaService : IGerdaService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly GerdaConfig _config;
    private readonly ILogger<GerdaService> _logger;
    private readonly IGroupingService _groupingService;
    private readonly IEstimatingService _estimatingService;
    private readonly IRankingService? _rankingService;
    private readonly IDispatchingService? _dispatchingService;
    private readonly IAnticipationService? _anticipationService;

    public GerdaService(
        ITicketRepository ticketRepository,
        GerdaConfig config,
        ILogger<GerdaService> logger,
        IGroupingService groupingService,
        IEstimatingService estimatingService,
        IRankingService? rankingService = null,
        IDispatchingService? dispatchingService = null,
        IAnticipationService? anticipationService = null)
    {
        _ticketRepository = ticketRepository;
        _config = config;
        _logger = logger;
        _groupingService = groupingService;
        _estimatingService = estimatingService;
        _rankingService = rankingService;
        _dispatchingService = dispatchingService;
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

            // R - Ranking: Calculate priority score (if service is available)
            if (_rankingService != null && _rankingService.IsEnabled)
            {
                var priorityScore = await _rankingService.CalculatePriorityScoreAsync(ticketGuid);
                _logger.LogInformation("GERDA-R: Ticket {TicketGuid} priority score: {Score}", ticketGuid, priorityScore);
            }

            // D - Dispatching: Recommend agent (if service is available)
            if (_dispatchingService != null && _dispatchingService.IsEnabled)
            {
                var recommendedAgent = await _dispatchingService.GetRecommendedAgentAsync(ticketGuid);
                if (recommendedAgent != null)
                {
                    _logger.LogInformation("GERDA-D: Recommended agent {AgentId} for ticket {TicketGuid}", recommendedAgent, ticketGuid);
                }
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

        // Use Repository to get all tickets (we might need a more specific method for open tickets later)
        // For now, fetching all and filtering in memory or adding a method to repo would be ideal.
        // Let's assume we fetch all and filter for now, or use a new repo method if available.
        // Checking ITicketRepository interface... it has GetAllAsync(departmentId).
        // We want ALL open tickets regardless of department for the background job.
        // Ideally we should add GetOpenTicketsAsync to the repository, but to avoid changing the interface too much right now,
        // let's use GetAllAsync(null) and filter.
        
        var allTickets = await _ticketRepository.GetAllAsync(null);
        var openTicketGuids = allTickets
            .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
            .Select(t => t.Guid)
            .ToList();

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
