using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.GERDA.Dispatching.Models;
using TicketMasala.Web.Engine.GERDA.Models;

namespace TicketMasala.Web.Engine.GERDA.Dispatching;

/// <summary>
/// ML.NET Matrix Factorization-based affinity scorer.
/// Extracted from MatrixFactorizationDispatchingStrategy as a focused adapter.
/// </summary>
public class MatrixFactorizationAffinityScorer : IAffinityScorer
{
    private readonly MasalaDbContext _context;
    private readonly ILogger<MatrixFactorizationAffinityScorer> _logger;
    private readonly PredictionEnginePool<AgentCustomerRating, RatingPrediction> _predictionEnginePool;
    private readonly MLContext _trainingContext;
    private readonly string _modelPath;

    public MatrixFactorizationAffinityScorer(
        MasalaDbContext context,
        ILogger<MatrixFactorizationAffinityScorer> logger,
        PredictionEnginePool<AgentCustomerRating, RatingPrediction> predictionEnginePool)
    {
        _context = context;
        _logger = logger;
        _predictionEnginePool = predictionEnginePool;
        _trainingContext = new MLContext(seed: 0);
        _modelPath = Path.Combine(AppContext.BaseDirectory, "gerda_dispatch_model.zip");
    }

    public string Name => "MatrixFactorization";

    public bool IsReady => File.Exists(_modelPath);

    public DateTime? LastTrained
    {
        get
        {
            if (File.Exists(_modelPath))
            {
                return File.GetLastWriteTimeUtc(_modelPath);
            }
            return null;
        }
    }

    public double CalculateAffinity(Employee employee, Ticket ticket, ApplicationUser? customer)
    {
        if (!IsReady)
        {
            _logger.LogWarning("GERDA-D: Model not ready, returning neutral affinity score");
            return 2.5; // Neutral score when model unavailable
        }

        if (string.IsNullOrEmpty(employee.Id))
        {
            return 0.0;
        }

        try
        {
            var customerId = ticket.CreatorGuid.ToString() ?? string.Empty;
            var input = new AgentCustomerRating
            {
                AgentId = employee.Id,
                CustomerId = customerId
            };

            var prediction = _predictionEnginePool.Predict("GerdaDispatchModel", input);
            return prediction.Score;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GERDA-D: Failed to predict affinity for agent {AgentId}, returning neutral", employee.Id);
            return 2.5;
        }
    }

    public string GetAffinityExplanation(double score, Employee employee, Ticket ticket)
    {
        if (score > 4.0)
        {
            return $"Strong historical affinity ({score:F1}/5) with customer on similar tickets";
        }
        else if (score > 3.0)
        {
            return $"Good historical affinity ({score:F1}/5) with customer";
        }
        else if (score > 2.0)
        {
            return $"Average historical affinity ({score:F1}/5)";
        }
        else
        {
            return $"Limited historical affinity ({score:F1}/5)";
        }
    }

    public async Task RetrainAsync()
    {
        _logger.LogInformation("GERDA-D: Starting affinity model retraining");

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
            AgentId = t.ResponsibleId ?? string.Empty,
            CustomerId = t.CreatorGuid.ToString() ?? string.Empty,
            Rating = CalculateImplicitRating(Enum.Parse<Status>(t.Status ?? "Pending"), t.CompletionDate, t.CreationDate)
        })
        .Where(a => !string.IsNullOrEmpty(a.AgentId))
        .ToList();

        const int minHistory = 10; // Minimum records for meaningful training
        if (trainingData.Count < minHistory)
        {
            _logger.LogWarning(
                "GERDA-D: Insufficient training data ({Count} records, need {Min}), skipping retraining",
                trainingData.Count, minHistory);
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
        _logger.LogInformation("GERDA-D: Affinity model retrained successfully with {Count} records", trainingData.Count);
    }

    private float CalculateImplicitRating(Status status, DateTime? completionDate, DateTime creationDate)
    {
        if (status == Status.Failed)
            return 1.0f;
        if (!completionDate.HasValue)
            return 3.0f;

        var resolutionTime = (completionDate.Value - creationDate).TotalHours;

        if (resolutionTime < 4)
            return 5.0f;
        if (resolutionTime < 24)
            return 4.0f;
        if (resolutionTime < 72)
            return 3.0f;
        if (resolutionTime < 168)
            return 2.0f;
        return 1.0f;
    }
}
