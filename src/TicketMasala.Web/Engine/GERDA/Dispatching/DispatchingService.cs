using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Engine.GERDA.Strategies;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace TicketMasala.Web.Engine.GERDA.Dispatching;

/// <summary>
/// D - Dispatching: Agent-ticket matching using ML.NET Matrix Factorization
/// Recommends the best agent for a ticket based on historical affinity and workload.
/// </summary>
public class DispatchingService : IDispatchingService
{
    private readonly MasalaDbContext _context;
    private readonly GerdaConfig _config;
    private readonly IStrategyFactory _strategyFactory;
    private readonly IDispatchingStrategySelector _strategySelector;
    private readonly IAutoDispatchPolicy _autoDispatchPolicy;
    private readonly IProjectManagerRecommendationService _projectManagerRecommendationService;
    private readonly ILogger<DispatchingService> _logger;

    public DispatchingService(
        MasalaDbContext context,
        GerdaConfig config,
        IStrategyFactory strategyFactory,
        IDispatchingStrategySelector strategySelector,
        IAutoDispatchPolicy autoDispatchPolicy,
        IProjectManagerRecommendationService projectManagerRecommendationService,
        ILogger<DispatchingService> logger)
    {
        _context = context;
        _config = config;
        _strategyFactory = strategyFactory;
        _strategySelector = strategySelector;
        _autoDispatchPolicy = autoDispatchPolicy;
        _projectManagerRecommendationService = projectManagerRecommendationService;
        _logger = logger;
    }

    public bool IsEnabled => _config.GerdaAI.IsEnabled && _config.GerdaAI.Dispatching.IsEnabled;

    public DateTime? LastModelTrainingTime
    {
        get
        {
            try
            {
                var strategyName = _strategySelector.GetDefaultStrategyName();
                var strategy = _strategyFactory.GetStrategy<IDispatchingStrategy, List<DispatchResult>>(strategyName);
                return strategy.LastTrained;
            }
            catch
            {
                return null;
            }
        }
    }

    public async Task<string?> GetRecommendedAgentAsync(Guid ticketGuid)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Dispatching service is disabled");
            return null;
        }

        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Guid == ticketGuid);

        if (ticket == null)
        {
            return null;
        }

        var recommendations = await GetTopRecommendedAgentsAsync(ticketGuid, count: 5);

        if (recommendations.Count == 0)
        {
            _logger.LogInformation("GERDA-D: No agent recommendations available for ticket {TicketGuid}, using fallback", ticketGuid);
            // Fallback is implicitly handled by strategy returning workload based or empty
            // Strategy implementation (MatrixFactorization) handles fallback to workload
            return null;
        }

        var bestAgent = recommendations.First().AgentId;
        _logger.LogInformation(
            "GERDA-D: Recommended agent {AgentId} for ticket {TicketGuid} with score {Score:F2}",
            bestAgent, ticketGuid, recommendations.First().Score);

        return bestAgent;
    }

    public async Task<List<DispatchResult>> GetTopRecommendedAgentsAsync(Guid ticketGuid, int count = 3)
    {
        if (!IsEnabled)
        {
            return new List<DispatchResult>();
        }

        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Guid == ticketGuid);

        if (ticket == null)
        {
            return new List<DispatchResult>();
        }

        // Determine Domain and Strategy
        var strategyName = _strategySelector.GetStrategyNameForTicket(ticket);

        try
        {
            var strategy = _strategyFactory.GetStrategy<IDispatchingStrategy, List<DispatchResult>>(strategyName);
            return await strategy.GetRecommendedAgentsAsync(ticket, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute dispatching strategy {StrategyName} for ticket {TicketGuid}", strategyName, ticketGuid);
            return new List<DispatchResult>();
        }
    }

    public async Task<bool> AutoDispatchTicketAsync(Guid ticketGuid)
    {
        if (!IsEnabled)
        {
            return false;
        }

        // Get top recommendation with score
        var recommendations = await GetTopRecommendedAgentsAsync(ticketGuid, 1);
        var bestMatch = recommendations.FirstOrDefault();

        if (!_autoDispatchPolicy.ShouldAutoDispatch(bestMatch, out var minScore))
        {
            _logger.LogInformation(
                "GERDA-D: Auto-dispatch skipped for {TicketGuid}. Best score {Score:F2} below threshold {MinScore:F2}",
                ticketGuid,
                bestMatch?.Score ?? 0,
                minScore);
            return false;
        }

        if (bestMatch == null)
        {
            return false;
        }

        var recommendedAgent = bestMatch.AgentId;

        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Guid == ticketGuid);
        if (ticket == null)
        {
            return false;
        }

        ticket.ResponsibleId = recommendedAgent;
        ticket.GerdaTags = string.IsNullOrEmpty(ticket.GerdaTags)
            ? "AI-Dispatched"
            : $"{ticket.GerdaTags},AI-Dispatched";

        await _context.SaveChangesAsync();

        _logger.LogInformation("GERDA-D: Auto-dispatched ticket {TicketGuid} to agent {AgentId} (Score: {Score:F2})", 
            ticketGuid, recommendedAgent, bestMatch.Score);
        return true;
    }

    public async Task RetrainModelAsync()
    {
        if (!IsEnabled)
        {
            return;
        }

        try
        {
            var strategyName = _strategySelector.GetDefaultStrategyName();
            var strategy = _strategyFactory.GetStrategy<IDispatchingStrategy, List<DispatchResult>>(strategyName);
            
            // Fire and forget background task
            _ = Task.Run(async () => 
            {
                try 
                {
                    await strategy.RetrainModelAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background model retraining failed");
                }
            });
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate model retraining");
            throw;
        }
    }



    /// <summary>
    /// Get recommended project manager for a ticket/project
    /// Uses workload balancing and historical project success
    /// </summary>
    public async Task<string?> GetRecommendedProjectManagerAsync(Guid ticketGuid)
    {
        if (!IsEnabled)
        {
            return null;
        }
        return await _projectManagerRecommendationService.GetRecommendedProjectManagerAsync(ticketGuid);
    }
}


