using TicketMasala.Web.Models;
using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Engine.GERDA.Features;
using TicketMasala.Web.Services.Configuration;
using TicketMasala.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace TicketMasala.Web.Engine.GERDA.Dispatching;
    public class MatrixFactorizationDispatchingStrategy : IDispatchingStrategy
    {
        public string Name => "MatrixFactorization";

        private readonly MasalaDbContext _context;
        private readonly GerdaConfig _config;
        private readonly ILogger<MatrixFactorizationDispatchingStrategy> _logger;
        private readonly MLContext _mlContext;
        private readonly IFeatureExtractor _featureExtractor;
        private readonly IDomainConfigurationService _domainConfig;
        private ITransformer? _model;
        private readonly string _modelPath;

        public MatrixFactorizationDispatchingStrategy(
            MasalaDbContext context,
            GerdaConfig config,
            ILogger<MatrixFactorizationDispatchingStrategy> logger,
            IFeatureExtractor featureExtractor,
            IDomainConfigurationService domainConfig)
        {
            _context = context;
            _config = config;
            _logger = logger;
            _featureExtractor = featureExtractor;
            _domainConfig = domainConfig;
            _mlContext = new MLContext(seed: 0);
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
                    var customer = await _context.Users.FindAsync(ticket.CreatorGuid.ToString());
                    var input = new AgentCustomerRating
                    {
                        AgentId = employee.Id,
                        CustomerId = ticket.CreatorGuid.ToString()
                    };

                    var prediction = predictionEngine.Predict(input);
                    
                    // Calculate multi-factor affinity score (4 factors: ML prediction, expertise, language, geography)
                    var multiFactorScore = AffinityScoring.CalculateMultiFactorScore(
                        prediction.Score,
                        ticket,
                        employee,
                        customer);
                    
                    // Adjust score based on current workload (penalize busy agents)
                    var workloadPenalty = currentWorkload / (double)_config.GerdaAI.Dispatching.MaxAssignedTicketsPerAgent;
                    var adjustedScore = multiFactorScore * (1.0 - (workloadPenalty * 0.5)); // Up to 50% penalty for full workload

                    _logger.LogDebug(
                        "GERDA-D: Agent {AgentName} scored {Score:F2} for ticket {TicketGuid} - {Explanation}",
                        $"{employee.FirstName} {employee.LastName}",
                        adjustedScore,
                        ticket.Guid,
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
                    CustomerId = t.CreatorGuid.ToString(),
                    t.Status,
                    t.CompletionDate,
                    t.CreationDate
                })
                .ToListAsync();

            var trainingData = rawTickets.Select(t => new AgentCustomerRating
            {
                AgentId = t.ResponsibleId!,
                CustomerId = t.CreatorGuid.ToString(),
                Rating = CalculateImplicitRating(t.Status, t.CompletionDate, t.CreationDate)
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
