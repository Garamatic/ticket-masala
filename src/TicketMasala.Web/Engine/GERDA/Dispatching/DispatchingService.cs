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
    /// </summary>
    private async Task<List<DispatchResult>> GetTopRecommendedAgentsConsolidatedAsync(Ticket ticket, int count)
    {
        // Get all employees as potential agents
        var employees = await _context.Users.OfType<Employee>().ToListAsync();

        if (employees.Count == 0)
        {
            _logger.LogWarning("GERDA-D: No employees found in system");
            return new List<DispatchResult>();
        }

        // Pre-load customer data ONCE (Issue #9 optimization)
        var customer = await _context.Users.FindAsync(ticket.CreatorGuid.ToString());

        // Get current workload for all agents
        var agentWorkloads = await _context.Tickets
            .Where(t => t.ResponsibleId != null)
            .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
            .GroupBy(t => t.ResponsibleId)
            .Select(g => new { AgentId = g.Key!, Count = g.Count() })
            .ToDictionaryAsync(x => x.AgentId!, x => x.Count);

        // Get ticket rowid for FTS5 optimization (Issue #9)
        long ticketRowId = 0;
        try
        {
            var rowIds = await _context.Database.SqlQueryRaw<long>(
                "SELECT rowid FROM Tickets WHERE Id = {0}", ticket.Guid)
                .ToListAsync();
            ticketRowId = rowIds.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GERDA-D: Failed to get RowId for FTS lookup");
        }

        // Convert employees to generic Agents
        var agents = ConvertEmployeesToAgents(employees, agentWorkloads);

        // Create work item adapter
        var workItem = new TicketWorkItemAdapter(ticket);

        // Get recommendations from consolidated engine
        var results = new List<DispatchResult>();

        foreach (var agent in agents.Where(a => a.IsAvailable))
        {
            // Find the corresponding employee
            var employee = employees.FirstOrDefault(e => e.Id == agent.Id);
            if (employee == null || string.IsNullOrEmpty(employee.Id))
            {
                continue;
            }

            // Calculate ML-based affinity score
            double mlAffinityScore = 2.5; // Default neutral
            string? affinityExplanation = null;
            try
            {
                mlAffinityScore = _affinityScorer.CalculateAffinity(employee, ticket, customer);
                affinityExplanation = _affinityScorer.GetAffinityExplanation(mlAffinityScore, employee, ticket);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GERDA-D: Affinity scoring failed for agent {AgentId}, using neutral", agent.Id);
            }

            // Calculate FTS5 skill match score (Issue #9: batch this)
            double ftsScore = 0;
            if (ticketRowId > 0 && !string.IsNullOrWhiteSpace(employee.Specializations))
            {
                ftsScore = await CalculateFtsScoreAsync(ticketRowId, employee.Specializations);
            }

            // Calculate workload factor
            var currentWorkload = agentWorkloads.GetValueOrDefault(agent.Id, 0);
            var workloadPenalty = currentWorkload / (double)_config.GerdaAI.Dispatching.MaxAssignedTicketsPerAgent;
            // Note: This uses the GerdaConfig value. The AgentMatchingEngine uses DispatchingConfig.MaxCasesPerAgent

            // Build generic match result using consolidated engine
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

        // Return top N by score
        return results
            .OrderByDescending(r => r.Score)
            .Take(count)
            .ToList();
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

    private async Task<double> CalculateFtsScoreAsync(long ticketRowId, string specializationsJson)
    {
        try
        {
            var specs = System.Text.Json.JsonSerializer.Deserialize<List<string>>(specializationsJson);
            if (specs == null || !specs.Any())
            {
                return 0;
            }

            // Build OR query: "spec1" OR "spec2"
            var matchQuery = string.Join(" OR ", specs.Select(s => $"\"{s.Replace("\"", "\"\"")}\""));

            var ranks = await _context.Database.SqlQueryRaw<double>(
                "SELECT rank FROM Tickets_Search WHERE rowid = {0} AND Tickets_Search MATCH {1}",
                ticketRowId, matchQuery)
                .ToListAsync();

            return ranks.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("GERDA-D: FTS scoring failed: {Message}", ex.Message);
            return 0;
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
