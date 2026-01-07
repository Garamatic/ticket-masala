using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Engine.GERDA.Strategies;
using TicketMasala.Web.Engine.GERDA.Configuration;
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
    private readonly IDomainConfigurationService _domainConfigService;
    private readonly ILogger<DispatchingService> _logger;

    public DispatchingService(
        MasalaDbContext context,
        GerdaConfig config,
        IStrategyFactory strategyFactory,
        IDomainConfigurationService domainConfigService,
        ILogger<DispatchingService> logger)
    {
        _context = context;
        _config = config;
        _strategyFactory = strategyFactory;
        _domainConfigService = domainConfigService;
        _logger = logger;
    }

    public bool IsEnabled => _config.GerdaAI.IsEnabled && _config.GerdaAI.Dispatching.IsEnabled;

    public DateTime? LastModelTrainingTime
    {
        get
        {
            try
            {
                var defaultDomainId = _domainConfigService.GetDefaultDomainId();
                var domainConfig = _domainConfigService.GetDomain(defaultDomainId);
                var strategyName = domainConfig?.AiStrategies.Dispatching ?? "MatrixFactorization";
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
        var domainId = ticket.DomainId ?? _domainConfigService.GetDefaultDomainId();
        var domainConfig = _domainConfigService.GetDomain(domainId);
        var strategyName = domainConfig?.AiStrategies.Dispatching ?? "MatrixFactorization";

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

        var recommendedAgent = await GetRecommendedAgentAsync(ticketGuid);

        if (string.IsNullOrEmpty(recommendedAgent))
        {
            return false;
        }

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

        _logger.LogInformation("GERDA-D: Auto-dispatched ticket {TicketGuid} to agent {AgentId}", ticketGuid, recommendedAgent);
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
            var defaultDomainId = _domainConfigService.GetDefaultDomainId();
            var domainConfig = _domainConfigService.GetDomain(defaultDomainId);
            var strategyName = domainConfig?.AiStrategies.Dispatching ?? "MatrixFactorization";
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
            _logger.LogDebug("Dispatching service is disabled");
            return null;
        }

        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Guid == ticketGuid);

        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketGuid} not found for PM recommendation", ticketGuid);
            return null;
        }

        // Get employees who could be project managers (all employees for now, could filter by role)
        var employees = await _context.Users.OfType<Employee>().ToListAsync();

        if (employees.Count == 0)
        {
            _logger.LogWarning("GERDA-D: No employees found for PM recommendation");
            return null;
        }

        // Get current project load for each employee (projects they manage)
        var pmProjectCounts = await _context.Projects
            .Where(p => p.ProjectManagerId != null)
            .Where(p => p.Status != Status.Completed && p.Status != Status.Failed)
            .GroupBy(p => p.ProjectManagerId)
            .Select(g => new { PMId = g.Key!, Count = g.Count() })
            .ToDictionaryAsync(x => x.PMId, x => x.Count);

        // Get historical success rate (completed projects)
        var pmSuccessRates = await _context.Projects
            .Where(p => p.ProjectManagerId != null)
            .Where(p => p.Status == Status.Completed || p.Status == Status.Failed)
            .GroupBy(p => p.ProjectManagerId)
            .Select(g => new
            {
                PMId = g.Key!,
                Total = g.Count(),
                Completed = g.Count(p => p.Status == Status.Completed)
            })
            .ToDictionaryAsync(
                x => x.PMId,
                x => x.Total > 0 ? (double)x.Completed / x.Total : 0.5);

        // Score each potential PM
        var scoredPMs = new List<(string PMId, double Score, string Name)>();
        const int maxProjectsPerPM = 5; // Configurable threshold

        foreach (var employee in employees)
        {
            var currentProjects = pmProjectCounts.GetValueOrDefault(employee.Id, 0);

            // Skip PMs who have too many active projects
            if (currentProjects >= maxProjectsPerPM)
            {
                continue;
            }

            // Calculate score based on:
            // 1. Workload (fewer projects = higher score)
            var workloadScore = 1.0 - (currentProjects / (double)maxProjectsPerPM);

            // 2. Historical success rate
            var successRate = pmSuccessRates.GetValueOrDefault(employee.Id, 0.5); // Default 50% for new PMs

            // 3. Combine factors (60% workload, 40% success rate)
            var combinedScore = (workloadScore * 0.6) + (successRate * 0.4);

            scoredPMs.Add((employee.Id, combinedScore, $"{employee.FirstName} {employee.LastName}"));

            _logger.LogDebug(
                "GERDA-D: PM {Name} scored {Score:F2} (workload: {Workload:F2}, success: {Success:F2})",
                $"{employee.FirstName} {employee.LastName}",
                combinedScore,
                workloadScore,
                successRate);
        }

        if (scoredPMs.Count == 0)
        {
            _logger.LogWarning("GERDA-D: All PMs at capacity, returning fallback");
            return await GetFallbackAgentAsync();
        }

        var bestPM = scoredPMs.OrderByDescending(x => x.Score).First();

        _logger.LogInformation(
            "GERDA-D: Recommended PM {Name} for ticket {TicketGuid} with score {Score:F2}",
            bestPM.Name, ticketGuid, bestPM.Score);

        return bestPM.PMId;
    }

    private async Task<string?> GetFallbackAgentAsync()
    {
        // Fallback: assign to agent with least current workload
        // Get all employees first
        var employees = await _context.Users.OfType<Employee>().ToListAsync();
        if (!employees.Any()) return null;

        var agentWorkloads = await _context.Tickets
            .Where(t => t.ResponsibleId != null)
            .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
            .GroupBy(t => t.ResponsibleId)
            .Select(g => new { AgentId = g.Key!, Count = g.Count() })
            .ToDictionaryAsync(x => x.AgentId, x => x.Count);

        var bestAgent = employees
            .Select(e => new { AgentId = e.Id, Count = agentWorkloads.GetValueOrDefault(e.Id, 0) })
            .OrderBy(x => x.Count)
            .FirstOrDefault();

        return bestAgent?.AgentId;
    }
}


