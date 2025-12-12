using TicketMasala.Web.Models;
using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Engine.GERDA.Features;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace TicketMasala.Web.Engine.GERDA.Dispatching;

using Microsoft.Extensions.ML;

// ...
public class MatrixFactorizationDispatchingStrategy : IDispatchingStrategy
{
    public string Name => "MatrixFactorization";

    private readonly MasalaDbContext _context;
    private readonly GerdaConfig _config;
    private readonly ILogger<MatrixFactorizationDispatchingStrategy> _logger;
    // private readonly MLContext _mlContext; // REMOVED: Using Pool
    private readonly PredictionEnginePool<AgentCustomerRating, RatingPrediction> _predictionEnginePool;
    private readonly IFeatureExtractor _featureExtractor;
    private readonly IDomainConfigurationService _domainConfig;

    // Needed for retraining only
    private readonly MLContext _trainingContext;
    private readonly string _modelPath;

    public MatrixFactorizationDispatchingStrategy(
        MasalaDbContext context,
        GerdaConfig config,
        ILogger<MatrixFactorizationDispatchingStrategy> logger,
        IFeatureExtractor featureExtractor,
        IDomainConfigurationService domainConfig,
        PredictionEnginePool<AgentCustomerRating, RatingPrediction> predictionEnginePool)
    {
        _context = context;
        _config = config;
        _logger = logger;
        _featureExtractor = featureExtractor;
        _domainConfig = domainConfig;
        _predictionEnginePool = predictionEnginePool;

        _trainingContext = new MLContext(seed: 0);
        _modelPath = Path.Combine(AppContext.BaseDirectory, "gerda_dispatch_model.zip");
    }

    public async Task<List<(string AgentId, double Score)>> GetRecommendedAgentsAsync(Ticket ticket, int count)
    {
        // Get all employees
        var employees = await _context.Users.OfType<Employee>().ToListAsync();

        if (employees.Count == 0)
        {
            _logger.LogWarning("GERDA-D: No employees found in system");
            return new List<(string, double)>();
        }

        // Feature Extraction Proof of Concept
        var domainId = ticket.DomainId ?? _domainConfig.GetDefaultDomainId();
        var domain = _domainConfig.GetDomain(domainId);

        if (domain != null && domain.AiModels.TryGetValue("dispatching", out var modelConfig))
        {
            var features = _featureExtractor.ExtractFeatures(ticket, modelConfig);
            _logger.LogInformation("GERDA-D: Extracted features for ticket {Guid}: [{Features}]",
                ticket.Guid, string.Join(", ", features.Select(f => f.ToString("F3"))));

            // TODO: In a real implementation with a trained generic model, we would pass 'features' 
            // to a PredictionEngine<FeatureVector, Prediction> here.
            // For now, we fall through to the Matrix Factorization / Workload logic.
        }

        // Load or train model if needed (for fallback/training purposes, though Pool handles prediction loading)
        // But we need to ensure the FILE exists for the pool to pick it up.
        await EnsureModelIsLoadedAsync();

        // We don't check _model here anymore, we check if file exists or trust the pool.
        if (!File.Exists(_modelPath))
        {
            _logger.LogWarning("GERDA-D: Model file not ready, using workload-based fallback");
            return await GetWorkloadBasedRecommendationsAsync(employees, count);
        }

        // Ensure model is available (via Pool/File)
        // Note: The pool handles loading. If file is missing, it might throw or return default.

        // Create prediction using the Pool
        try
        {
            // Get current workload for each agent
            var now = DateTime.UtcNow;
            var agentWorkloads = await _context.Tickets
                .Where(t => t.ResponsibleId != null)
                .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
                .GroupBy(t => t.ResponsibleId)
                .Select(g => new { AgentId = g.Key!, Count = g.Count() })
                .ToDictionaryAsync(x => x.AgentId!, x => x.Count);

            // Score each agent
            var scoredAgents = new List<(string AgentId, double Score)>();

            // Fetch customer for context (Affinity Scoring)
            var customer = await _context.Users.FindAsync(ticket.CreatorGuid.ToString());

            // Optimization: Get internal RowId of the ticket for efficient FTS lookup (avoid scanning Tickets_Search)
            // SQLite RowId is hidden for tables with non-integer PKs
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
                    AgentId = employee.Id!, // Employee.Id is required, should not be null
                    CustomerId = ticket.CreatorGuid.ToString()
                };

                // SCALABILITY: Use the pool. No creation overhead.
                var prediction = _predictionEnginePool.Predict("GerdaDispatchModel", input);

                // Compute FTS5 Match Score (Factor 2 Support)
                double ftsScore = 0;
                if (ticketRowId > 0 && !string.IsNullOrWhiteSpace(employee.Specializations))
                {
                    try
                    {
                        var specs = System.Text.Json.JsonSerializer.Deserialize<List<string>>(employee.Specializations);
                        if (specs != null && specs.Any())
                        {
                            // Build OR query: "spec1" OR "spec2"
                            var matchQuery = string.Join(" OR ", specs.Select(s => $"\"{s.Replace("\"", "\"\"")}\""));

                            // Query rank from FTS5 table targeting specific ticket row
                            // Note: Using string interpolation for table name in SqlQueryRaw is risky if table name dynamic, but here it's constant.
                            // Parameterizing matchQuery is tricky with MATCH syntax in some providers, but usually safe as param.
                            // SQLite FTS MATCH expects simple string.
                            var ranks = await _context.Database.SqlQueryRaw<double>(
                                "SELECT rank FROM Tickets_Search WHERE rowid = {0} AND Tickets_Search MATCH {1}",
                                ticketRowId, matchQuery)
                                .ToListAsync();

                            ftsScore = ranks.FirstOrDefault();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log rare parsing/FTS errors but don't crash dispatch
                        _logger.LogWarning("GERDA-D: FTS scoring failed for agent {Agent}: {Message}", employee.Id, ex.Message);
                    }
                }

                // Calculate multi-factor affinity score (4 factors: ML prediction, expertise, language, geography)
                var multiFactorScore = AffinityScoring.CalculateMultiFactorScore(
                    prediction.Score,
                    ticket,
                    employee,
                    customer,
                    ftsScore); // Pass FTS score

                // Adjust score based on current workload (penalize busy agents)
                var workloadPenalty = currentWorkload / (double)_config.GerdaAI.Dispatching.MaxAssignedTicketsPerAgent;
                var adjustedScore = multiFactorScore * (1.0 - (workloadPenalty * 0.5)); // Up to 50% penalty for full workload

                _logger.LogDebug(
                    "GERDA-D: Agent {AgentName} scored {Score:F2} for ticket {TicketGuid} (FTS: {Fts:F2})",
                    $"{employee.FirstName} {employee.LastName}",
                    adjustedScore,
                    ticket.Guid,
                    ftsScore);

                scoredAgents.Add((employee.Id!, adjustedScore));
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

    public async Task RetrainModelAsync()
    {
        _logger.LogInformation("GERDA-D: Starting model retraining");

        // Get historical ticket assignments with completion data
        var rawTickets = await _context.Tickets
            .Where(t => t.ResponsibleId != null)
            .Where(t => t.Status == "Completed" || t.Status == "Failed")
            .Select(t => new
            {
                t.ResponsibleId,
                t.CreatorGuid,
                t.Status,
                t.CompletionDate,
                t.CreationDate
            })
            .ToListAsync();

        var trainingData = rawTickets.Select(t => new AgentCustomerRating
        {
            AgentId = t.ResponsibleId!, // Already filtered for non-null ResponsibleId
            CustomerId = t.CreatorGuid.ToString(),
            Rating = CalculateImplicitRating(Enum.Parse<Status>(t.Status ?? "Pending"), t.CompletionDate, t.CreationDate)
        }).ToList();

        if (trainingData.Count < _config.GerdaAI.Dispatching.MinHistoryForAffinityMatch)
        {
            _logger.LogWarning(
                "GERDA-D: Insufficient training data ({Count} records, need {Min}), skipping retraining",
                trainingData.Count, _config.GerdaAI.Dispatching.MinHistoryForAffinityMatch);
            return;
        }

        var dataView = _trainingContext.Data.LoadFromEnumerable(trainingData);

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

        var pipeline = _trainingContext.Transforms.Conversion
            .MapValueToKey("AgentIdEncoded", "AgentId")
            .Append(_trainingContext.Transforms.Conversion.MapValueToKey("CustomerIdEncoded", "CustomerId"))
            .Append(_trainingContext.Recommendation().Trainers.MatrixFactorization(options));

        // Train the model
        var trainedModel = pipeline.Fit(dataView);

        // Save the model
        _trainingContext.Model.Save(trainedModel, dataView.Schema, _modelPath);

        // Note: PredictionEnginePool detects file changes via watchForChanges: true, so it will reload automatically!
        _logger.LogInformation("GERDA-D: Model retrained successfully with {Count} records", trainingData.Count);
    }

    private async Task EnsureModelIsLoadedAsync()
    {
        if (File.Exists(_modelPath))
        {
            return;
        }

        // No model exists, train a new one
        await RetrainModelAsync();
    }

    private float CalculateImplicitRating(Status status, DateTime? completionDate, DateTime creationDate)
    {
        if (status == Status.Failed) return 1.0f;
        if (!completionDate.HasValue) return 3.0f;

        var resolutionTime = (completionDate.Value - creationDate).TotalHours;

        if (resolutionTime < 4) return 5.0f;
        if (resolutionTime < 24) return 4.0f;
        if (resolutionTime < 72) return 3.0f;
        if (resolutionTime < 168) return 2.0f;
        return 1.0f;
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
