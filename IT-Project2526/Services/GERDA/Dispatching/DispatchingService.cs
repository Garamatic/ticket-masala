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

        return scoredAgents
            .OrderByDescending(x => x.Score)
            .Take(count)
            .ToList();
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
        var trainingData = await _context.Tickets
            .Where(t => t.ResponsibleId != null && t.CustomerId != null)
            .Where(t => t.TicketStatus == Status.Completed || t.TicketStatus == Status.Failed)
            .Select(t => new AgentCustomerRating
            {
                AgentId = t.ResponsibleId!,
                CustomerId = t.CustomerId!,
                Rating = CalculateImplicitRating(t)
            })
            .ToListAsync();

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

    private float CalculateImplicitRating(Ticket ticket)
    {
        // Implicit rating based on:
        // - Ticket was completed (not failed) = positive signal
        // - Resolution speed = faster is better
        
        if (ticket.TicketStatus == Status.Failed)
        {
            return 1.0f; // Negative signal
        }

        if (!ticket.CompletionDate.HasValue)
        {
            return 3.0f; // Neutral - completed but no date
        }

        var resolutionTime = (ticket.CompletionDate.Value - ticket.CreationDate).TotalHours;
        
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
        var agentWorkloads = await _context.Tickets
            .Where(t => t.ResponsibleId != null)
            .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
            .GroupBy(t => t.ResponsibleId)
            .Select(g => new { AgentId = g.Key!, Count = g.Count() })
            .OrderBy(x => x.Count)
            .FirstOrDefaultAsync();

        return agentWorkloads?.AgentId;
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
