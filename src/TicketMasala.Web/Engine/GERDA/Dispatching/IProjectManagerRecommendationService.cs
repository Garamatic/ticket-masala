using Microsoft.EntityFrameworkCore;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.GERDA.Models;

namespace TicketMasala.Web.Engine.GERDA.Dispatching;

public interface IProjectManagerRecommendationService
{
    Task<string?> GetRecommendedProjectManagerAsync(Guid ticketGuid);
}

public sealed class WorkloadAndSuccessProjectManagerRecommendationService : IProjectManagerRecommendationService
{
    private readonly MasalaDbContext _context;
    private readonly GerdaConfig _config;
    private readonly ILogger<WorkloadAndSuccessProjectManagerRecommendationService> _logger;

    public WorkloadAndSuccessProjectManagerRecommendationService(
        MasalaDbContext context,
        GerdaConfig config,
        ILogger<WorkloadAndSuccessProjectManagerRecommendationService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    public async Task<string?> GetRecommendedProjectManagerAsync(Guid ticketGuid)
    {
        if (!_config.GerdaAI.IsEnabled || !_config.GerdaAI.Dispatching.IsEnabled)
        {
            _logger.LogDebug("Dispatching service is disabled");
            return null;
        }

        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Guid == ticketGuid);
        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketGuid} not found for PM recommendation", ticketGuid);
            return null;
        }

        var employees = await _context.Users.OfType<Employee>().ToListAsync();
        if (employees.Count == 0)
        {
            _logger.LogWarning("GERDA-D: No employees found for PM recommendation");
            return null;
        }

        var maxActiveProjects = _config.GerdaAI.Dispatching.ProjectManagerMaxActiveProjects;
        if (maxActiveProjects <= 0)
        {
            maxActiveProjects = 5;
        }

        var workloadWeight = _config.GerdaAI.Dispatching.ProjectManagerWorkloadWeight;
        var successWeight = _config.GerdaAI.Dispatching.ProjectManagerSuccessRateWeight;
        var weightSum = workloadWeight + successWeight;
        if (weightSum <= 0)
        {
            workloadWeight = 0.6;
            successWeight = 0.4;
            weightSum = 1.0;
        }

        workloadWeight /= weightSum;
        successWeight /= weightSum;

        var pmProjectCounts = await _context.Projects
            .Where(p => p.ProjectManagerId != null)
            .Where(p => p.Status != Status.Completed && p.Status != Status.Failed)
            .GroupBy(p => p.ProjectManagerId)
            .Select(g => new { PMId = g.Key!, Count = g.Count() })
            .ToDictionaryAsync(x => x.PMId, x => x.Count);

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

        var scoredPMs = new List<(string PMId, double Score, string Name)>();

        foreach (var employee in employees)
        {
            var currentProjects = pmProjectCounts.GetValueOrDefault(employee.Id, 0);
            if (currentProjects >= maxActiveProjects)
            {
                continue;
            }

            var workloadScore = 1.0 - (currentProjects / (double)maxActiveProjects);
            var successRate = pmSuccessRates.GetValueOrDefault(employee.Id, 0.5);
            var combinedScore = (workloadScore * workloadWeight) + (successRate * successWeight);

            var name = $"{employee.FirstName} {employee.LastName}";
            scoredPMs.Add((employee.Id, combinedScore, name));

            _logger.LogDebug(
                "GERDA-D: PM {Name} scored {Score:F2} (workload: {Workload:F2}, success: {Success:F2})",
                name,
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
            bestPM.Name,
            ticketGuid,
            bestPM.Score);

        return bestPM.PMId;
    }

    private async Task<string?> GetFallbackAgentAsync()
    {
        var employees = await _context.Users.OfType<Employee>().ToListAsync();
        if (employees.Count == 0)
        {
            return null;
        }

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
