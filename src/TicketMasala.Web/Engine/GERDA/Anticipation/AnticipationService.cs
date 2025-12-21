using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;

namespace TicketMasala.Web.Engine.GERDA.Anticipation;

/// <summary>
/// A - Anticipation: Ticket inflow forecasting using ML.NET Time Series SSA
/// Predicts future ticket volume to identify capacity risks and alert managers.
/// </summary>
public class AnticipationService : IAnticipationService
{
    private readonly MasalaDbContext _context;
    private readonly GerdaConfig _config;
    private readonly ILogger<AnticipationService> _logger;
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private readonly string _modelPath;

    public AnticipationService(
        MasalaDbContext context,
        GerdaConfig config,
        ILogger<AnticipationService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
        _mlContext = new MLContext(seed: 0);
        _modelPath = Path.Combine(AppContext.BaseDirectory, "gerda_anticipation_model.zip");
    }

    public bool IsEnabled => _config.GerdaAI.IsEnabled && _config.GerdaAI.Anticipation.IsEnabled;

    public async Task<List<(DateTime Date, int PredictedCount)>> ForecastInflowAsync(int horizonDays = 30)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Anticipation service is disabled");
            return new List<(DateTime, int)>();
        }

        // Get historical daily ticket counts
        var historicalData = await GetHistoricalTicketCountsAsync();

        if (historicalData.Count < _config.GerdaAI.Anticipation.MinHistoryForForecasting)
        {
            _logger.LogWarning(
                "GERDA-A: Insufficient historical data ({Count} days, need {Min}), cannot forecast",
                historicalData.Count, _config.GerdaAI.Anticipation.MinHistoryForForecasting);
            return new List<(DateTime, int)>();
        }

        var dataView = _mlContext.Data.LoadFromEnumerable(historicalData);

        // SSA (Singular Spectrum Analysis) forecasting pipeline
        var forecastingPipeline = _mlContext.Forecasting.ForecastBySsa(
            outputColumnName: "ForecastedTickets",
            inputColumnName: "TicketCount",
            windowSize: 30, // 30-day window for pattern detection
            seriesLength: historicalData.Count,
            trainSize: historicalData.Count,
            horizon: horizonDays,
            confidenceLevel: 0.95f,
            confidenceLowerBoundColumn: "LowerBound",
            confidenceUpperBoundColumn: "UpperBound");

        // Train the model
        _model = forecastingPipeline.Fit(dataView);

        // Save the model
        _mlContext.Model.Save(_model, dataView.Schema, _modelPath);

        // Create forecast
        var forecastEngine = _model.CreateTimeSeriesEngine<TicketInflowData, TicketInflowForecast>(_mlContext);
        var forecast = forecastEngine.Predict();

        // Build result with dates
        var results = new List<(DateTime Date, int PredictedCount)>();
        var startDate = DateTime.UtcNow.Date.AddDays(1); // Forecast starts tomorrow

        for (int i = 0; i < horizonDays; i++)
        {
            var date = startDate.AddDays(i);
            var predictedCount = (int)Math.Round(forecast.ForecastedTickets[i]);
            results.Add((date, predictedCount));
        }

        _logger.LogInformation(
            "GERDA-A: Generated {Days}-day forecast based on {HistoryCount} days of historical data",
            horizonDays, historicalData.Count);

        return results;
    }

    public async Task<double> GetTeamCapacityAsync(Guid? projectGuid = null)
    {
        if (!IsEnabled)
        {
            return 0;
        }

        // Calculate total team capacity: number of agents * max tickets per agent
        var employeeCount = await _context.Users.OfType<Employee>().CountAsync();
        var capacity = employeeCount * _config.GerdaAI.Dispatching.MaxAssignedTicketsPerAgent;

        _logger.LogDebug("GERDA-A: Current team capacity = {Capacity} tickets ({Employees} agents)",
            capacity, employeeCount);

        return capacity;
    }

    public async Task<CapacityRiskResult?> CheckCapacityRiskAsync()
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Anticipation service is disabled");
            return null;
        }

        // Get 7-day forecast
        var forecast = await ForecastInflowAsync(horizonDays: 7);

        if (forecast.Count == 0)
        {
            _logger.LogWarning("GERDA-A: Insufficient data for forecasting");
            return null;
        }

        // Calculate average predicted daily inflow
        var avgPredictedDailyInflow = forecast.Average(f => f.PredictedCount);

        // Get current team capacity
        var dailyCapacity = await GetTeamCapacityAsync();

        // Calculate expected weekly inflow
        var weeklyPredictedInflow = (int)(avgPredictedDailyInflow * 7);

        // Get current open ticket count (backlog)
        var currentBacklog = await _context.Tickets
            .Where(t => t.TicketStatus != Status.Completed && t.TicketStatus != Status.Failed)
            .CountAsync();

        // Total load = backlog + predicted new tickets
        var totalLoad = currentBacklog + weeklyPredictedInflow;
        var weeklyCapacity = dailyCapacity * 7;

        // Check if we're exceeding capacity threshold
        var utilizationRate = totalLoad / weeklyCapacity;
        var riskThreshold = _config.GerdaAI.Anticipation.RiskThresholdPercentage / 100.0;

        if (utilizationRate > riskThreshold)
        {
            var riskPercentage = utilizationRate * 100;
            var alertMessage = $"Capacity risk detected: {utilizationRate:P0} utilization. " +
                              $"Current backlog: {currentBacklog} tickets. " +
                              $"Predicted weekly inflow: {weeklyPredictedInflow} tickets. " +
                              $"Weekly capacity: {weeklyCapacity} tickets. " +
                              $"Consider hiring additional agents or prioritizing tickets.";

            _logger.LogWarning("GERDA-A: {Message}", alertMessage);

            return new CapacityRiskResult
            {
                RiskStartDate = DateTime.UtcNow.Date,
                ForecastedInflow = weeklyPredictedInflow,
                AvailableCapacity = weeklyCapacity,
                RiskPercentage = riskPercentage,
                AlertMessage = alertMessage
            };
        }

        _logger.LogInformation("GERDA-A: Capacity is healthy: {Utilization:P0} utilization (threshold: {Threshold:P0})",
            utilizationRate, riskThreshold);

        return null;
    }

    public async Task<List<(DateTime Date, int ActualTickets, float PredictedTickets)>> GetForecastAccuracyAsync()
    {
        if (!IsEnabled)
        {
            return new List<(DateTime, int, float)>();
        }

        // Get historical data for the past 30 days
        var thirtyDaysAgo = DateTime.UtcNow.Date.AddDays(-30);

        var actualCounts = await _context.Tickets
            .Where(t => t.CreationDate >= thirtyDaysAgo)
            .GroupBy(t => t.CreationDate.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToDictionaryAsync(x => x.Date, x => x.Count);

        // For each historical day, generate what the forecast would have been
        // (This is simplified - in production you'd store historical predictions)
        var results = new List<(DateTime Date, int ActualTickets, float PredictedTickets)>();

        foreach (var kvp in actualCounts)
        {
            // Use moving average as simple baseline predictor for accuracy comparison
            var priorDays = actualCounts
                .Where(x => x.Key < kvp.Key && x.Key >= kvp.Key.AddDays(-7))
                .Select(x => x.Value)
                .ToList();

            var predictedCount = priorDays.Count > 0 ? priorDays.Average() : 0;
            results.Add((kvp.Key, kvp.Value, (float)predictedCount));
        }

        if (results.Count > 0)
        {
            var mae = results.Average(r => Math.Abs(r.ActualTickets - r.PredictedTickets));
            _logger.LogInformation("GERDA-A: Mean Absolute Error over last 30 days: {MAE:F2} tickets", mae);
        }

        return results;
    }

    private async Task<List<TicketInflowData>> GetHistoricalTicketCountsAsync()
    {
        // Get all historical tickets grouped by creation date
        var ticketCounts = await _context.Tickets
            .GroupBy(t => t.CreationDate.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync();

        // Convert to ML.NET format
        return ticketCounts.Select(tc => new TicketInflowData
        {
            Date = tc.Date,
            TicketCount = tc.Count
        }).ToList();
    }
}

/// <summary>
/// Input data for ML.NET Time Series forecasting
/// </summary>
public class TicketInflowData
{
    public DateTime Date { get; set; }
    public float TicketCount { get; set; }
}

/// <summary>
/// Forecast output from ML.NET SSA model
/// </summary>
public class TicketInflowForecast
{
    [VectorType]
    public float[] ForecastedTickets { get; set; } = Array.Empty<float>();

    [VectorType]
    public float[] LowerBound { get; set; } = Array.Empty<float>();

    [VectorType]
    public float[] UpperBound { get; set; } = Array.Empty<float>();

}
