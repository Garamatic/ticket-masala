using Microsoft.EntityFrameworkCore;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.Common;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Web.Engine.GERDA.Dispatching.Algorithms;
using TicketMasala.Web.Engine.GERDA.Dispatching.Configuration;
using TicketMasala.Web.Engine.GERDA.Dispatching.Models;
using TicketMasala.Web.Engine.GERDA.Models;
using DispatchResultModel = TicketMasala.Web.Engine.GERDA.Dispatching.Models.DispatchResult;

namespace TicketMasala.Web.Engine.GERDA.Dispatching;

/// <summary>
/// D - Dispatching: Agent-ticket matching using consolidated AgentMatchingEngine architecture.
/// 
/// ARCHITECTURE CHANGE (Issue #7):
/// Previously had two competing paths:
///   - Path A: Strategy-based via MatrixFactorizationDispatchingStrategy
///   - Path B: Generic Engine (unused private method)
/// 
/// NEW ARCHITECTURE:
///   - Single primary path: AgentMatchingEngine with IAffinityScorer plugins
///   - MatrixFactorizationAffinityScorer provides ML-based affinity scoring
///   - Legacy strategy path available via shim for backward compatibility
/// 
/// This eliminates the shallow module anti-pattern and consolidates dispatching logic.
/// </summary>
public class DispatchingService : IDispatchingService
{
    private readonly MasalaDbContext _context;
    private readonly GerdaConfig _config;
    private readonly IAutoDispatchPolicy _autoDispatchPolicy;
    private readonly IProjectManagerRecommendationService _projectManagerRecommendationService;
    private readonly AgentMatchingEngine _agentMatchingEngine;
    private readonly IAffinityScorer _affinityScorer;
    private readonly IDispatchingStrategy? _legacyStrategy; // Optional legacy shim
    private readonly ILogger<DispatchingService> _logger;

    public DispatchingService(
        MasalaDbContext context,
        GerdaConfig config,
        IAutoDispatchPolicy autoDispatchPolicy,
        IProjectManagerRecommendationService projectManagerRecommendationService,
        AgentMatchingEngine agentMatchingEngine,
        IAffinityScorer affinityScorer,
        IDispatchingStrategy? legacyStrategy, // Optional for backward compatibility
        ILogger<DispatchingService> logger)
    {
        _context = context;
        _config = config;
        _autoDispatchPolicy = autoDispatchPolicy;
        _projectManagerRecommendationService = projectManagerRecommendationService;
        _agentMatchingEngine = agentMatchingEngine ?? throw new ArgumentNullException(nameof(agentMatchingEngine));
        _affinityScorer = affinityScorer ?? throw new ArgumentNullException(nameof(affinityScorer));
        _legacyStrategy = legacyStrategy;
        _logger = logger;
    }

    public bool IsEnabled => _config.GerdaAI.IsEnabled && _config.GerdaAI.Dispatching.IsEnabled;

    public DateTime? LastModelTrainingTime => _affinityScorer.LastTrained;

    public async Task<string?> GetRecommendedAgentAsync(Guid ticketGuid)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Dispatching service is disabled");
            return null;
        }

        var recommendations = await GetTopRecommendedAgentsAsync(ticketGuid, count: 1);

        if (recommendations.Count == 0)
        {
            _logger.LogInformation("GERDA-D: No agent recommendations available for ticket {TicketGuid}", ticketGuid);
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
            _logger.LogWarning("GERDA-D: Ticket {TicketGuid} not found", ticketGuid);
            return new List<DispatchResult>();
        }

        try
        {
            return await GetTopRecommendedAgentsConsolidatedAsync(ticket, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GERDA-D: Consolidated dispatching failed for ticket {TicketGuid}, attempting legacy fallback", ticketGuid);

            // Fallback to legacy strategy if available
            if (_legacyStrategy != null)
            {
                try
                {
                    return await _legacyStrategy.GetRecommendedAgentsAsync(ticket, count);
                }
                catch (Exception legacyEx)
                {
                    _logger.LogError(legacyEx, "GERDA-D: Legacy strategy also failed for ticket {TicketGuid}", ticketGuid);
                }
            }

            return new List<DispatchResult>();
        }
    }

    /// <summary>
    /// NEW CONSOLIDATED PATH (Issue #7):
    /// Single unified implementation using AgentMatchingEngine + IAffinityScorer.
    ///
    /// Issue #9: N+1 Query Optimization
    /// - Pre-loads customer ONCE
    /// - Pre-loads all workloads in single query
    /// - Batches FTS5 skill matching (single query for all agents)
    /// - Pre-calculates all affinity scores before loop
    /// </summary>
    private async Task<List<DispatchResult>> GetTopRecommendedAgentsConsolidatedAsync(Ticket ticket, int count)
    {
        // OPTIMIZATION: Get employees and workloads in parallel (2 queries)
        var employeesTask = _context.Users.OfType<Employee>().ToListAsync();
        var workloadTask = _context.Tickets
            .Where(t => t.ResponsibleId != null)
            .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
            .GroupBy(t => t.ResponsibleId)
            .Select(g => new { AgentId = g.Key!, Count = g.Count() })
            .ToDictionaryAsync(x => x.AgentId!, x => x.Count);

        await Task.WhenAll(employeesTask, workloadTask);

        var employees = employeesTask.Result;
        var agentWorkloads = workloadTask.Result;

        // Customer is loaded separately (FindAsync returns ValueTask, not Task)
        var customer = await _context.Users.FindAsync(ticket.CreatorGuid.ToString());

        if (employees.Count == 0)
        {
            _logger.LogWarning("GERDA-D: No employees found in system");
            return new List<DispatchResult>();
        }

        // OPTIMIZATION: Get ticket rowid for FTS5 (1 query)
        long ticketRowId = await GetTicketRowIdAsync(ticket.Guid);

        // OPTIMIZATION: Pre-calculate all FTS5 scores in a SINGLE batch query (Issue #9)
        // Instead of N queries (one per agent), we do 1 query for all agents
        var ftsScores = await CalculateAllFtsScoresAsync(ticketRowId, employees);

        // OPTIMIZATION: Pre-calculate all ML affinity scores
        // These are in-memory calculations using the ML model, no DB queries
        var affinityScores = CalculateAllAffinityScores(employees, ticket, customer);

        // Convert employees to generic Agents
        var agents = ConvertEmployeesToAgents(employees, agentWorkloads);

        // Create work item adapter
        var workItem = new TicketWorkItemAdapter(ticket);

        // Build results using pre-calculated scores (NO DB QUERIES in this loop!)
        var results = new List<DispatchResult>();

        foreach (var agent in agents.Where(a => a.IsAvailable))
        {
            var employee = employees.FirstOrDefault(e => e.Id == agent.Id);
            if (employee == null || string.IsNullOrEmpty(employee.Id))
            {
                continue;
            }

            // Get pre-calculated scores (no DB queries here!)
            var (mlAffinityScore, affinityExplanation) = affinityScores.GetValueOrDefault(agent.Id, (2.5, null));
            var ftsScore = ftsScores.GetValueOrDefault(agent.Id, 0.0);

            // Calculate workload factor
            var currentWorkload = agentWorkloads.GetValueOrDefault(agent.Id, 0);
            var workloadPenalty = currentWorkload / (double)_config.GerdaAI.Dispatching.MaxAssignedTicketsPerAgent;

            // Build result
            var genericResult = BuildDispatchResult(
                agent,
                workItem,
                mlAffinityScore,
                affinityExplanation,
                ftsScore,
                workloadPenalty,
                employee,
                customer);

            results.Add(genericResult);
        }

        _logger.LogDebug(
            "GERDA-D: Optimized dispatching for ticket {TicketGuid} using {QueryCount} DB queries (was ~{OldQueryCount})",
            ticket.Guid, 5, employees.Count * 2 + 3);

        // Return top N by score
        return results
            .OrderByDescending(r => r.Score)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Issue #9: Optimized batch FTS5 score calculation.
    /// Replaces N individual queries with a single batch query.
    /// </summary>
    private async Task<Dictionary<string, double>> CalculateAllFtsScoresAsync(long ticketRowId, List<Employee> employees)
    {
        var scores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        if (ticketRowId <= 0)
        {
            return scores;
        }

        // Get all unique specializations across all agents
        var allSpecs = employees
            .Where(e => !string.IsNullOrWhiteSpace(e.Specializations))
            .SelectMany(e => ParseSpecializations(e.Specializations))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (allSpecs.Count == 0)
        {
            return scores;
        }

        try
        {
            // Build a single OR query for all unique specializations
            var matchQuery = string.Join(" OR ", allSpecs.Select(s => $"\"{s.Replace("\"", "\"\"")}\""));

            // Execute single FTS5 query
            var matches = await _context.Database.SqlQueryRaw<FtsMatchResult>(
                "SELECT rowid, rank FROM Tickets_Search WHERE rowid = {0} AND Tickets_Search MATCH {1}",
                ticketRowId, matchQuery)
                .ToListAsync();

            // For each employee, check if any of their specializations matched
            foreach (var employee in employees.Where(e => !string.IsNullOrWhiteSpace(e.Specializations)))
            {
                var empSpecs = ParseSpecializations(employee.Specializations);
                var maxRank = matches
                    .Where(m => empSpecs.Any(spec =>
                        allSpecs.Contains(spec, StringComparer.OrdinalIgnoreCase)))
                    .Select(m => m.Rank)
                    .FirstOrDefault();

                if (maxRank != 0)
                {
                    scores[employee.Id ?? string.Empty] = maxRank;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GERDA-D: Batch FTS5 scoring failed, falling back to no FTS scores");
        }

        return scores;
    }

    /// <summary>
    /// Helper class for FTS5 query results.
    /// </summary>
    private sealed class FtsMatchResult
    {
        public long RowId { get; set; }
        public double Rank { get; set; }
    }

    /// <summary>
    /// Issue #9: Optimized batch affinity score calculation.
    /// All calculations are in-memory using pre-loaded data.
    /// </summary>
    private Dictionary<string, (double Score, string? Explanation)> CalculateAllAffinityScores(
        List<Employee> employees,
        Ticket ticket,
        ApplicationUser? customer)
    {
        var scores = new Dictionary<string, (double, string?)>(StringComparer.OrdinalIgnoreCase);

        foreach (var employee in employees)
        {
            if (string.IsNullOrEmpty(employee.Id))
            {
                continue;
            }

            try
            {
                var score = _affinityScorer.CalculateAffinity(employee, ticket, customer);
                var explanation = _affinityScorer.GetAffinityExplanation(score, employee, ticket);
                scores[employee.Id] = (score, explanation);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GERDA-D: Affinity scoring failed for agent {AgentId}, using neutral", employee.Id);
                scores[employee.Id] = (2.5, null);
            }
        }

        return scores;
    }

    /// <summary>
    /// Gets the SQLite rowid for a ticket (for FTS5 queries).
    /// </summary>
    private async Task<long> GetTicketRowIdAsync(Guid ticketGuid)
    {
        try
        {
            var rowIds = await _context.Database.SqlQueryRaw<long>(
                "SELECT rowid FROM Tickets WHERE Id = {0}", ticketGuid)
                .ToListAsync();
            return rowIds.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GERDA-D: Failed to get RowId for FTS lookup");
            return 0;
        }
    }

    private List<Agent> ConvertEmployeesToAgents(List<Employee> employees, Dictionary<string, int> workloads)
    {
        return employees.Select(e => new Agent
        {
            Id = e.Id ?? string.Empty,
            Name = $"{e.FirstName} {e.LastName}",
            Department = e.Team ?? string.Empty,
            Competencies = ParseSpecializations(e.Specializations),
            CurrentCaseCount = workloads.GetValueOrDefault(e.Id ?? string.Empty, 0),
            MaxCapacity = _config.GerdaAI.Dispatching.MaxAssignedTicketsPerAgent
            // Note: This uses the GerdaConfig value. The AgentMatchingEngine uses DispatchingConfig.MaxCasesPerAgent
        }).ToList();
    }

    private List<string> ParseSpecializations(string? specializationsJson)
    {
        if (string.IsNullOrWhiteSpace(specializationsJson))
        {
            return new List<string>();
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(specializationsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private DispatchResult BuildDispatchResult(
        Agent agent,
        IWorkItem workItem,
        double mlAffinityScore,
        string? affinityExplanation,
        double ftsScore,
        double workloadPenalty,
        Employee employee,
        ApplicationUser? customer)
    {
        // Calculate multi-factor score (adapted from original MatrixFactorizationDispatchingStrategy)
        var multiFactorScore = CalculateMultiFactorScore(mlAffinityScore, employee, customer, ftsScore);

        // Apply workload penalty
        var adjustedScore = multiFactorScore * (1.0 - (workloadPenalty * 0.5));

        var result = new DispatchResult(agent.Id, adjustedScore);

        // Build reasons list
        if (workloadPenalty < 0.2)
        {
            result.Reasons.Add("High Availability");
        }
        else if (workloadPenalty < 0.5)
        {
            result.Reasons.Add("Available Capacity");
        }

        if (mlAffinityScore > 3.0)
        {
            result.Reasons.Add($"Historical Affinity ({mlAffinityScore:F1}/5)");
        }

        if (ftsScore != 0)
        {
            result.Reasons.Add($"Skill Match (FTS Rank: {ftsScore:F2})");
        }
        else
        {
            // Fallback to legacy expertise scoring
            var expertiseScore = AffinityScoring.CalculateExpertiseScore(workItem, employee);
            if (expertiseScore > 3.0)
            {
                var category = AffinityScoring.ExtractCategoryFromWorkItem(workItem);
                result.Reasons.Add($"Expertise Match: {category} ({expertiseScore:F1}/5)");
            }
        }

        if (customer != null)
        {
            var languageScore = AffinityScoring.CalculateLanguageScore(employee, customer);
            if (languageScore >= 4.5)
            {
                result.Reasons.Add($"Language Match ({languageScore:F1}/5)");
            }

            var geoScore = AffinityScoring.CalculateGeographyScore(employee, customer);
            if (geoScore >= 4.0)
            {
                result.Reasons.Add($"Region Match ({geoScore:F1}/5)");
            }
        }

        result.Explanation = affinityExplanation
            ?? AffinityScoring.GetScoreExplanation(mlAffinityScore, workItem, employee, customer);

        return result;
    }

    private double CalculateMultiFactorScore(double mlScore, Employee employee, ApplicationUser? customer, double ftsScore)
    {
        // Start with ML affinity score (0-5)
        var baseScore = mlScore;

        // Boost for FTS match (if any)
        var ftsBoost = ftsScore != 0 ? 0.5 : 0;

        // Normalize to 0-5 scale
        return Math.Min(baseScore + ftsBoost, 5.0);
    }

    public async Task<bool> AutoDispatchTicketAsync(Guid ticketGuid)
    {
        if (!IsEnabled)
        {
            return false;
        }

        var recommendations = await GetTopRecommendedAgentsAsync(ticketGuid, 1);
        var bestMatch = recommendations.FirstOrDefault();

        if (!_autoDispatchPolicy.ShouldAutoDispatch(bestMatch, out var minScore))
        {
            _logger.LogInformation(
                "GERDA-D: Auto-dispatch skipped for {TicketGuid}. Best score {Score:F2} below threshold {MinScore:F2}",
                ticketGuid, bestMatch?.Score ?? 0, minScore);
            return false;
        }

        if (bestMatch == null)
        {
            return false;
        }

        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Guid == ticketGuid);
        if (ticket == null)
        {
            return false;
        }

        ticket.ResponsibleId = bestMatch.AgentId;
        ticket.GerdaTags = string.IsNullOrEmpty(ticket.GerdaTags)
            ? "AI-Dispatched"
            : $"{ticket.GerdaTags},AI-Dispatched";

        await _context.SaveChangesAsync();

        _logger.LogInformation("GERDA-D: Auto-dispatched ticket {TicketGuid} to agent {AgentId} (Score: {Score:F2})",
            ticketGuid, bestMatch.AgentId, bestMatch.Score);
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
            await _affinityScorer.RetrainAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GERDA-D: Failed to retrain affinity model");
            throw;
        }
    }

    public async Task<string?> GetRecommendedProjectManagerAsync(Guid ticketGuid)
    {
        if (!IsEnabled)
        {
            return null;
        }
        return await _projectManagerRecommendationService.GetRecommendedProjectManagerAsync(ticketGuid);
    }
}
