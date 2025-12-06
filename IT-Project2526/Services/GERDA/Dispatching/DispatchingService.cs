using IT_Project2526.Services.GERDA.Models;
using IT_Project2526.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace IT_Project2526.Services.GERDA.Dispatching;

/// <summary>
/// D - Dispatching: Agent-ticket matching using ML.NET Matrix Factorization
/// Recommends the best agent for a ticket based on historical affinity and workload.
/// </summary>
public class DispatchingService : IDispatchingService
{
    private readonly ITProjectDB _context;
    private readonly GerdaConfig _config;
    private readonly ILogger<DispatchingService> _logger;
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private readonly string _modelPath;

    public DispatchingService(
        ITProjectDB context,
        GerdaConfig config,
        ILogger<DispatchingService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
        _mlContext = new MLContext(seed: 0);
        _modelPath = Path.Combine(AppContext.BaseDirectory, "gerda_dispatch_model.zip");
    }

    public bool IsEnabled => _config.GerdaAI.IsEnabled && _config.GerdaAI.Dispatching.IsEnabled;

    public async Task<string?> GetRecommendedAgentAsync(Guid ticketGuid)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Dispatching service is disabled");
            return null;
        }

        var ticket = await _context.Tickets
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.Guid == ticketGuid);

        if (ticket == null)
        {
            _logger.LogWarning("Ticket {TicketGuid} not found for dispatching", ticketGuid);
            return null;
        }

        if (ticket.Customer == null || string.IsNullOrEmpty(ticket.CustomerId))
        {
            _logger.LogWarning("Ticket {TicketGuid} has no customer, cannot dispatch", ticketGuid);
            return null;
        }

        var recommendations = await GetTopRecommendedAgentsAsync(ticketGuid, count: 5);
        
        if (recommendations.Count == 0)
        {
            _logger.LogInformation("GERDA-D: No agent recommendations available for ticket {TicketGuid}, using fallback", ticketGuid);
            return await GetFallbackAgentAsync();
        }

        var bestAgent = recommendations.First().AgentId;
        _logger.LogInformation(
            "GERDA-D: Recommended agent {AgentId} for ticket {TicketGuid} with score {Score:F2}",
            bestAgent, ticketGuid, recommendations.First().Score);

        return bestAgent;
    }

    public async Task<List<(string AgentId, double Score)>> GetTopRecommendedAgentsAsync(Guid ticketGuid, int count = 3)
    {
        if (!IsEnabled)
        {
            return new List<(string, double)>();
        }

        var ticket = await _context.Tickets
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.Guid == ticketGuid);

        if (ticket?.Customer == null)
        {
            return new List<(string, double)>();
        }

        // Get all employees
        var employees = await _context.Users.OfType<Employee>().ToListAsync();
        
        if (employees.Count == 0)
        {
            _logger.LogWarning("GERDA-D: No employees found in system");
            return new List<(string, double)>();
        }

        // Load or train model if needed
        await EnsureModelIsLoadedAsync();

        if (_model == null)
        {
            _logger.LogWarning("GERDA-D: Model not available, using workload-based fallback");
            return await GetWorkloadBasedRecommendationsAsync(employees, count);
        }

        // Create prediction engine
        try 
        {
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<AgentCustomerRating, RatingPrediction>(_model);

            // Get current workload for each agent
            var now = DateTime.UtcNow;
            var agentWorkloads = await _context.Tickets
                .Where(t => t.ResponsibleId != null)
                .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
                .GroupBy(t => t.ResponsibleId)
                .Select(g => new { AgentId = g.Key!, Count = g.Count() })
                .ToDictionaryAsync(x => x.AgentId, x => x.Count);

            // Score each agent
            var scoredAgents = new List<(string AgentId, double Score)>();

            foreach (var employee in employees)
            {
                var currentWorkload = agentWorkloads.GetValueOrDefault(employee.Id, 0);

                // Skip agents who are at max capacity
                if (currentWorkload >= _config.GerdaAI.Dispatching.MaxAssignedTicketsPerAgent)
                {
                    continue;
                }

                // Predict affinity score using ML model (Factor 1: Past Interaction)
                var input = new AgentCustomerRating
                {
                    AgentId = employee.Id,
                    CustomerId = ticket.CustomerId!
                };

                var prediction = predictionEngine.Predict(input);
                
                // Calculate multi-factor affinity score (4 factors: ML prediction, expertise, language, geography)
                var multiFactorScore = AffinityScoring.CalculateMultiFactorScore(
                    prediction.Score,
                    ticket,
                    employee,
                    ticket.Customer);
                
                // Adjust score based on current workload (penalize busy agents)
                var workloadPenalty = currentWorkload / (double)_config.GerdaAI.Dispatching.MaxAssignedTicketsPerAgent;
                var adjustedScore = multiFactorScore * (1.0 - (workloadPenalty * 0.5)); // Up to 50% penalty for full workload

                _logger.LogDebug(
                    "GERDA-D: Agent {AgentName} scored {Score:F2} for ticket {TicketGuid} - {Explanation}",
                    $"{employee.FirstName} {employee.LastName}",
                    adjustedScore,
                    ticketGuid,
                    AffinityScoring.GetScoreExplanation(prediction.Score, ticket, employee, ticket.Customer));

                scoredAgents.Add((employee.Id, adjustedScore));
            }

            var results = scoredAgents
                .OrderByDescending(x => x.Score)
                .Take(count)
                .ToList();

            if (results.Count > 0)
            {
                return results;
            }
            
            _logger.LogWarning("GERDA-D: Model returned no results, falling back to workload");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GERDA-D: Prediction failed, falling back to workload");
        }

        // Fallback if model fails or returns no results
        return await GetWorkloadBasedRecommendationsAsync(employees, count);
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
            _logger.LogDebug("Dispatching service is disabled, skipping model retraining");
            return;
        }

        _logger.LogInformation("GERDA-D: Starting model retraining");

        // Get historical ticket assignments with completion data
        // Get historical ticket assignments with completion data
        var rawTickets = await _context.Tickets
            .Where(t => t.ResponsibleId != null && t.CustomerId != null)
            .Where(t => t.TicketStatus == Status.Completed || t.TicketStatus == Status.Failed)
            .Select(t => new 
            {
                t.ResponsibleId,
                t.CustomerId,
                t.TicketStatus,
                t.CompletionDate,
                t.CreationDate
            })
            .ToListAsync();

        var trainingData = rawTickets.Select(t => new AgentCustomerRating
        {
            AgentId = t.ResponsibleId!,
            CustomerId = t.CustomerId!,
            Rating = CalculateImplicitRating(t.TicketStatus, t.CompletionDate, t.CreationDate)
        }).ToList();

        if (trainingData.Count < _config.GerdaAI.Dispatching.MinHistoryForAffinityMatch)
        {
            _logger.LogWarning(
                "GERDA-D: Insufficient training data ({Count} records, need {Min}), skipping retraining",
                trainingData.Count, _config.GerdaAI.Dispatching.MinHistoryForAffinityMatch);
            return;
        }

        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        // Matrix Factorization training pipeline
        var options = new MatrixFactorizationTrainer.Options
        {
            MatrixColumnIndexColumnName = "AgentIdEncoded",
            MatrixRowIndexColumnName = "CustomerIdEncoded",
            LabelColumnName = "Rating",
            NumberOfIterations = 20,
            ApproximationRank = 10,
            LearningRate = 0.1,
            Quiet = true
        };

        var pipeline = _mlContext.Transforms.Conversion
            .MapValueToKey("AgentIdEncoded", "AgentId")
            .Append(_mlContext.Transforms.Conversion.MapValueToKey("CustomerIdEncoded", "CustomerId"))
            .Append(_mlContext.Recommendation().Trainers.MatrixFactorization(options));

        // Train the model
        _model = pipeline.Fit(dataView);

        // Save the model
        _mlContext.Model.Save(_model, dataView.Schema, _modelPath);

        _logger.LogInformation("GERDA-D: Model retrained successfully with {Count} records", trainingData.Count);
    }

    private async Task EnsureModelIsLoadedAsync()
    {
        if (_model != null)
        {
            return; // Already loaded
        }

        if (File.Exists(_modelPath))
        {
            try
            {
                _model = _mlContext.Model.Load(_modelPath, out _);
                _logger.LogInformation("GERDA-D: Model loaded from {Path}", _modelPath);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GERDA-D: Failed to load model, will retrain");
            }
        }

        // No model exists, train a new one
        await RetrainModelAsync();
    }

    private float CalculateImplicitRating(Status status, DateTime? completionDate, DateTime creationDate)
    {
        // Implicit rating based on:
        // - Ticket was completed (not failed) = positive signal
        // - Resolution speed = faster is better
        
        if (status == Status.Failed)
        {
            return 1.0f; // Negative signal
        }

        if (!completionDate.HasValue)
        {
            return 3.0f; // Neutral - completed but no date
        }

        var resolutionTime = (completionDate.Value - creationDate).TotalHours;
        
        // Rating scale 1-5 based on resolution time
        // < 4 hours = excellent (5)
        // < 24 hours = good (4)
        // < 72 hours = average (3)
        // < 168 hours (1 week) = below average (2)
        // > 1 week = poor (1)
        
        if (resolutionTime < 4) return 5.0f;
        if (resolutionTime < 24) return 4.0f;
        if (resolutionTime < 72) return 3.0f;
        if (resolutionTime < 168) return 2.0f;
        return 1.0f;
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

    private async Task<List<(string AgentId, double Score)>> GetWorkloadBasedRecommendationsAsync(
        List<Employee> employees, int count)
    {
        var agentWorkloads = await _context.Tickets
            .Where(t => t.ResponsibleId != null)
            .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
            .GroupBy(t => t.ResponsibleId)
            .Select(g => new { AgentId = g.Key!, Count = g.Count() })
            .ToDictionaryAsync(x => x.AgentId, x => x.Count);

        return employees
            .Select(e => (
                AgentId: e.Id,
                Score: 1.0 - (agentWorkloads.GetValueOrDefault(e.Id, 0) / (double)_config.GerdaAI.Dispatching.MaxAssignedTicketsPerAgent)
            ))
            .OrderByDescending(x => x.Score)
            .Take(count)
            .ToList();
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
            .Include(t => t.Customer)
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
}

/// <summary>
/// Input data for ML.NET Matrix Factorization model
/// </summary>
public class AgentCustomerRating
{
    public string AgentId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public float Rating { get; set; } // 1-5 scale
}

/// <summary>
/// Prediction output from ML.NET model
/// </summary>
public class RatingPrediction
{
    public float Score { get; set; }
}
