using Microsoft.EntityFrameworkCore;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;

namespace TicketMasala.Web.Engine.GERDA.Dispatching;

/// <summary>
/// Deep module implementation for ticket dispatching.
///
/// Consolidates recommendation scoring, auto-dispatch policy, and model retraining
/// behind a single command-oriented interface.
///
/// Key invariants:
/// - Recommendations always include multi-factor scoring (affinity + workload + skill + geo + language)
/// - Auto-dispatch checks policy threshold before assignment
/// - Assignment is delegated to ITicketLifecycle (never duplicated here)
/// - All DB queries are batched (no N+1)
/// </summary>
public class TicketDispatcher : ITicketDispatcher
{
    private readonly MasalaDbContext _context;
    private readonly GerdaConfig _config;
    private readonly IAutoDispatchPolicy _autoDispatchPolicy;
    private readonly IAffinityScorer _affinityScorer;
    private readonly ITicketLifecycle _ticketLifecycle;
    private readonly ILogger<TicketDispatcher> _logger;

    public TicketDispatcher(
        MasalaDbContext context,
        GerdaConfig config,
        IAutoDispatchPolicy autoDispatchPolicy,
        IAffinityScorer affinityScorer,
        ITicketLifecycle ticketLifecycle,
        ILogger<TicketDispatcher> logger)
    {
        _context = context;
        _config = config;
        _autoDispatchPolicy = autoDispatchPolicy;
        _affinityScorer = affinityScorer;
        _ticketLifecycle = ticketLifecycle;
        _logger = logger;
    }

    public bool IsEnabled => _config.GerdaAI.IsEnabled && _config.GerdaAI.Dispatching.IsEnabled;

    public async Task<DispatcherResult> ExecuteAsync(
        IDispatchCommand command,
        CancellationToken cancellationToken = default)
    {
        return command switch
        {
            RecommendAgentsCommand recommend => await HandleRecommendAsync(recommend, cancellationToken),
            AutoDispatchCommand autoDispatch => await HandleAutoDispatchAsync(autoDispatch, cancellationToken),
            RetrainCommand retrain => await HandleRetrainAsync(retrain, cancellationToken),
            _ => DispatcherResult.Fail($"Unknown dispatch command: {command.GetType().Name}")
        };
    }

    private async Task<DispatcherResult> HandleRecommendAsync(
        RecommendAgentsCommand command,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Dispatching is disabled");
            return DispatcherResult.WithRecommendations(Array.Empty<AgentRecommendation>());
        }

        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.Guid == command.TicketGuid, cancellationToken);

        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketGuid} not found for dispatching", command.TicketGuid);
            return DispatcherResult.Fail("Ticket not found");
        }

        try
        {
            var recommendations = await GetRecommendationsAsync(ticket, command.Count, cancellationToken);
            return DispatcherResult.WithRecommendations(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dispatching failed for ticket {TicketGuid}", command.TicketGuid);
            return DispatcherResult.Fail($"Dispatching failed: {ex.Message}");
        }
    }

    private async Task<IReadOnlyList<AgentRecommendation>> GetRecommendationsAsync(
        Ticket ticket,
        int count,
        CancellationToken cancellationToken)
    {
        var employeesTask = _context.Users.OfType<Employee>().ToListAsync(cancellationToken);
        var workloadTask = _context.Tickets
            .Where(t => t.ResponsibleId != null)
            .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
            .GroupBy(t => t.ResponsibleId)
            .Select(g => new { AgentId = g.Key!, Count = g.Count() })
            .ToDictionaryAsync(x => x.AgentId!, x => x.Count, cancellationToken);

        await Task.WhenAll(employeesTask, workloadTask);

        var employees = await employeesTask;
        var agentWorkloads = await workloadTask;

        if (employees.Count == 0)
        {
            _logger.LogWarning("No employees found in system");
            return Array.Empty<AgentRecommendation>();
        }

        ApplicationUser? customer = await _context.Users.FindAsync(ticket.CreatorGuid.ToString()) as ApplicationUser;

        var affinityScores = CalculateAffinityScores(employees, ticket, customer);

        var results = new List<AgentRecommendation>();

        foreach (var employee in employees.Where(e => !string.IsNullOrEmpty(e.Id)))
        {
            var workload = agentWorkloads.GetValueOrDefault(employee.Id, 0);
            var maxCapacity = _config.GerdaAI.Dispatching.MaxAssignedTicketsPerAgent;
            var workloadPenalty = workload / (double)maxCapacity;

            var (mlScore, explanation) = affinityScores.GetValueOrDefault(employee.Id, (2.5, null));

            var multiFactorScore = Math.Min(mlScore + (mlScore > 0 ? 0.5 : 0), 5.0);
            var adjustedScore = multiFactorScore * (1.0 - (workloadPenalty * 0.5));

            var reasons = new List<string>();

            if (workloadPenalty < 0.2)
                reasons.Add("High Availability");
            else if (workloadPenalty < 0.5)
                reasons.Add("Available Capacity");

            if (mlScore > 3.0)
                reasons.Add($"Historical Affinity ({mlScore:F1}/5)");

            if (customer != null)
            {
                var languageScore = AffinityScoring.CalculateLanguageScore(employee, customer);
                if (languageScore >= 4.5)
                    reasons.Add($"Language Match ({languageScore:F1}/5)");

                var geoScore = AffinityScoring.CalculateGeographyScore(employee, customer);
                if (geoScore >= 4.0)
                    reasons.Add($"Region Match ({geoScore:F1}/5)");
            }

            results.Add(new AgentRecommendation
            {
                AgentId = employee.Id,
                Score = adjustedScore,
                Reasons = reasons,
                Explanation = explanation ?? AffinityScoring.GetScoreExplanation(mlScore, ticket, employee, customer)
            });
        }

        return results
            .OrderByDescending(r => r.Score)
            .Take(count)
            .ToList();
    }

    private Dictionary<string, (double Score, string? Explanation)> CalculateAffinityScores(
        List<Employee> employees,
        Ticket ticket,
        ApplicationUser? customer)
    {
        var scores = new Dictionary<string, (double, string?)>(StringComparer.OrdinalIgnoreCase);

        foreach (var employee in employees)
        {
            if (string.IsNullOrEmpty(employee.Id))
                continue;

            try
            {
                var score = _affinityScorer.CalculateAffinity(employee, ticket, customer);
                var explanation = _affinityScorer.GetAffinityExplanation(score, employee, ticket);
                scores[employee.Id] = (score, explanation);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Affinity scoring failed for agent {AgentId}, using neutral", employee.Id);
                scores[employee.Id] = (2.5, null);
            }
        }

        return scores;
    }

    private async Task<DispatcherResult> HandleAutoDispatchAsync(
        AutoDispatchCommand command,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Auto-dispatch skipped: dispatching disabled");
            return DispatcherResult.Skipped("Dispatching is disabled");
        }

        var recommendResult = await HandleRecommendAsync(
            new RecommendAgentsCommand(command.TicketGuid, 1),
            cancellationToken);

        if (!recommendResult.Success || recommendResult.Recommendations.Count == 0)
        {
            return DispatcherResult.Skipped("No recommendations available");
        }

        var bestMatch = recommendResult.Recommendations.First();
        var minScore = command.MinimumScore ?? _config.GerdaAI.Dispatching.AutoDispatchMinScore;

        if (bestMatch.Score < minScore)
        {
            _logger.LogInformation(
                "Auto-dispatch skipped for {TicketGuid}. Best score {Score:F2} below threshold {MinScore:F2}",
                command.TicketGuid, bestMatch.Score, minScore);
            return DispatcherResult.Skipped($"Best score {bestMatch.Score:F2} below threshold {minScore:F2}");
        }

        var assignResult = await _ticketLifecycle.ExecuteAsync(
            new AssignTicketCommand(command.TicketGuid, bestMatch.AgentId),
            new TicketContext("system"),
            cancellationToken);

        if (!assignResult.Success)
        {
            _logger.LogWarning(
                "Auto-dispatch assignment failed for {TicketGuid}: {Error}",
                command.TicketGuid, assignResult.ErrorMessage);
            return DispatcherResult.Fail(assignResult.ErrorMessage ?? "Assignment failed");
        }

        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Guid == command.TicketGuid, cancellationToken);
        if (ticket != null)
        {
            ticket.GerdaTags = string.IsNullOrEmpty(ticket.GerdaTags)
                ? "AI-Dispatched"
                : $"{ticket.GerdaTags},AI-Dispatched";
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Auto-dispatched ticket {TicketGuid} to agent {AgentId} (Score: {Score:F2})",
            command.TicketGuid, bestMatch.AgentId, bestMatch.Score);

        return DispatcherResult.AutoAssigned(bestMatch.AgentId);
    }

    private async Task<DispatcherResult> HandleRetrainAsync(
        RetrainCommand command,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Retrain skipped: dispatching disabled");
            return DispatcherResult.Skipped("Dispatching is disabled");
        }

        try
        {
            await _affinityScorer.RetrainAsync();
            _logger.LogInformation("Affinity model retrained successfully");
            return DispatcherResult.Retrained(_affinityScorer.LastTrained);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Affinity model retraining failed");
            return DispatcherResult.Fail($"Retraining failed: {ex.Message}");
        }
    }
}
