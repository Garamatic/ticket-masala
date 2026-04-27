using Microsoft.EntityFrameworkCore;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.Common;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Web.Engine.GERDA.Dispatching.Algorithms;
using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Engine.GERDA.Strategies;

namespace TicketMasala.Web.Engine.GERDA.Ranking;


/// <summary>
/// R - Ranking: WSJF (Weighted Shortest Job First) priority calculation
/// Calculates priority score: Cost of Delay / Job Size
/// 
/// Now delegates to Engine.GERDA.Dispatching.WsjfEngine for generic algorithm.
/// Maintains backward compatibility with domain-specific strategies.
/// </summary>
public class RankingService : IRankingService
{
    private readonly MasalaDbContext _context;
    private readonly GerdaConfig _config;
    private readonly IStrategyFactory _strategyFactory;
    private readonly IDomainConfigurationService _domainConfigService;
    private readonly WsjfEngine _wsjfEngine;
    private readonly ILogger<RankingService> _logger;

    public RankingService(
        MasalaDbContext context,
        GerdaConfig config,
        IStrategyFactory strategyFactory,
        IDomainConfigurationService domainConfigService,
        WsjfEngine wsjfEngine,
        ILogger<RankingService> logger)
    {
        _context = context;
        _config = config;
        _strategyFactory = strategyFactory;
        _domainConfigService = domainConfigService;
        _wsjfEngine = wsjfEngine ?? throw new ArgumentNullException(nameof(wsjfEngine));
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
        var strategyName = domainConfig?.AiStrategies.Ranking?.StrategyName ?? "WSJF";

        double priorityScore = 0.0;

        try
        {
            // Option 1: Use upstream WsjfEngine for standard WSJF calculations
            if (strategyName == "WSJF")
            {
                var workItem = new TicketWorkItemAdapter(ticket);
                var result = _wsjfEngine.CalculatePriority(workItem);
                priorityScore = (double)result.WsjfScore;
                _logger.LogDebug(
                    "GERDA-R: Using WsjfEngine for ticket {TicketGuid}, score {Score:F2}",
                    ticketGuid, priorityScore);
            }
            else
            {
                // Option 2: Use domain-specific strategy for other algorithms
                var strategy = _strategyFactory.GetStrategy<IJobRankingStrategy, double>(strategyName);
                priorityScore = strategy.CalculateScore(ticket, _config);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute ranking strategy {StrategyName} for ticket {TicketGuid}", strategyName, ticketGuid);
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
