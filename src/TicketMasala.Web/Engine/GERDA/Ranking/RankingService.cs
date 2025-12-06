using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Engine.GERDA.Ranking;

using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Engine.GERDA.Strategies;
using TicketMasala.Web.Services.Configuration;
using TicketMasala.Web.Models;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// R - Ranking: WSJF (Weighted Shortest Job First) priority calculation
/// Calculates priority score: Cost of Delay / Job Size
/// </summary>
public class RankingService : IRankingService
{
    private readonly ITProjectDB _context;
    private readonly GerdaConfig _config;
    private readonly IStrategyFactory _strategyFactory;
    private readonly IDomainConfigurationService _domainConfigService;
    private readonly ILogger<RankingService> _logger;

    public RankingService(
        ITProjectDB context,
        GerdaConfig config,
        IStrategyFactory strategyFactory,
        IDomainConfigurationService domainConfigService,
        ILogger<RankingService> logger)
    {
        _context = context;
        _config = config;
        _strategyFactory = strategyFactory;
        _domainConfigService = domainConfigService;
        _logger = logger;
    }

    public bool IsEnabled => _config.GerdaAI.IsEnabled && _config.GerdaAI.Ranking.IsEnabled;

    public async Task<double> CalculatePriorityScoreAsync(Guid ticketGuid)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Ranking service is disabled");
            return 0.0;
        }

        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Guid == ticketGuid);
        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketGuid} not found for ranking", ticketGuid);
            return 0.0;
        }

        // Determine Domain and Strategy
        var domainId = ticket.DomainId ?? _domainConfigService.GetDefaultDomainId();
        var domainConfig = _domainConfigService.GetDomain(domainId);
        var strategyName = domainConfig?.AiStrategies.Ranking ?? "WSJF";

        double priorityScore = 0.0;

        try 
        {
            var strategy = _strategyFactory.GetStrategy<IJobRankingStrategy, double>(strategyName);
            priorityScore = strategy.CalculateScore(ticket, _config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute ranking strategy {StrategyName} for ticket {TicketGuid}", strategyName, ticketGuid);
            // Fallback to 0 or rethrow? For now, 0 safe fallback but log error.
            return 0.0;
        }

        // Update the ticket
        ticket.PriorityScore = priorityScore;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "GERDA-R: Ticket {TicketGuid} ranked with priority score {Score:F2} using strategy {StrategyName}",
            ticketGuid, priorityScore, strategyName);

        return priorityScore;
    }

    public async Task RecalculateAllPrioritiesAsync()
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Ranking service is disabled, skipping recalculation");
            return;
        }

        _logger.LogInformation("GERDA-R: Starting priority recalculation for all open tickets");

        var openTickets = await _context.Tickets
            .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
            .ToListAsync();

        foreach (var ticket in openTickets)
        {
            try
            {
                await CalculatePriorityScoreAsync(ticket.Guid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GERDA-R: Failed to recalculate priority for ticket {TicketGuid}", ticket.Guid);
            }
        }

        _logger.LogInformation("GERDA-R: Completed priority recalculation for {Count} tickets", openTickets.Count);
    }

    public async Task<List<Guid>> GetPrioritizedTicketGuidsAsync(Guid? projectGuid = null)
    {
        var query = _context.Tickets
            .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed);

        if (projectGuid.HasValue)
        {
            query = query.Where(t => t.ProjectGuid == projectGuid.Value);
        }

        var prioritizedGuids = await query
            .OrderByDescending(t => t.PriorityScore)
            .ThenBy(t => t.CreationDate) // Tie-breaker: older tickets first
            .Select(t => t.Guid)
            .ToListAsync();

        return prioritizedGuids;

    }

}
