using IT_Project2526.Models;
using IT_Project2526.Services.GERDA.Ranking;
using IT_Project2526.Services.GERDA.Dispatching;
using Microsoft.EntityFrameworkCore;

namespace IT_Project2526.Services.GERDA.BackgroundJobs
{
    /// <summary>
    /// Background service for GERDA AI maintenance tasks
    /// - Priority recalculation every 6 hours
    /// - ML model retraining daily at 2 AM
    /// </summary>
    public class GerdaBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<GerdaBackgroundService> _logger;
        private readonly TimeSpan _priorityRecalculationInterval = TimeSpan.FromHours(6);
        private readonly TimeSpan _modelRetrainingCheckInterval = TimeSpan.FromHours(1);

        public GerdaBackgroundService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<GerdaBackgroundService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GERDA Background Service started");

            var lastPriorityRecalculation = DateTime.UtcNow;
            var lastModelRetrainingDate = DateTime.UtcNow.Date;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;

                    // Priority Recalculation (every 6 hours)
                    if (now - lastPriorityRecalculation >= _priorityRecalculationInterval)
                    {
                        await RecalculateAllPriorities(stoppingToken);
                        lastPriorityRecalculation = now;
                    }

                    // Model Retraining (daily at 2 AM UTC)
                    if (now.Date > lastModelRetrainingDate && now.Hour == 2)
                    {
                        await RetrainDispatchingModel(stoppingToken);
                        lastModelRetrainingDate = now.Date;
                    }

                    // Wait 1 hour before next check
                    await Task.Delay(_modelRetrainingCheckInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Service is stopping, exit gracefully
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in GERDA Background Service main loop");
                    // Continue running despite errors
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("GERDA Background Service stopped");
        }

        /// <summary>
        /// Recalculate priority scores for all active tickets
        /// This ensures urgency is updated as tickets age and SLA deadlines approach
        /// </summary>
        private async Task RecalculateAllPriorities(CancellationToken stoppingToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var rankingService = scope.ServiceProvider.GetRequiredService<IRankingService>();

            try
            {
                _logger.LogInformation("Starting priority recalculation for all active tickets");

                await rankingService.RecalculateAllPrioritiesAsync();

                _logger.LogInformation("Priority recalculation completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error during priority recalculation");
            }
        }

        /// <summary>
        /// Retrain the ML.NET Matrix Factorization model for agent dispatching
        /// Uses completed ticket data to improve future recommendations
        /// </summary>
        private async Task RetrainDispatchingModel(CancellationToken stoppingToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dispatchingService = scope.ServiceProvider.GetRequiredService<IDispatchingService>();

            try
            {
                _logger.LogInformation("Starting ML model retraining for dispatching service");

                await dispatchingService.RetrainModelAsync();

                _logger.LogInformation("ML model retraining completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error during ML model retraining");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("GERDA Background Service is stopping");
            await base.StopAsync(cancellationToken);
        }
    }
}
