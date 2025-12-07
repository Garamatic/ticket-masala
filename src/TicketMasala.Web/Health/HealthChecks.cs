using Microsoft.Extensions.Diagnostics.HealthChecks;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Engine.Ingestion.Background;
using TicketMasala.Web.Engine.GERDA.Models;

namespace TicketMasala.Web.Health;

/// <summary>
/// Health check for GERDA AI services.
/// Verifies that GERDA configuration is loaded and services are operational.
/// </summary>
public class GerdaHealthCheck : IHealthCheck
{
    private readonly GerdaConfig? _config;

    public GerdaHealthCheck(IServiceProvider serviceProvider)
    {
        _config = serviceProvider.GetService<GerdaConfig>();
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_config == null)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "GERDA configuration not loaded - AI features disabled"));
        }

        if (!_config.GerdaAI.IsEnabled)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "GERDA is disabled in configuration"));
        }

        var data = new Dictionary<string, object>
        {
            { "SpamDetection", _config.GerdaAI.SpamDetection?.IsEnabled ?? false },
            { "ComplexityEstimation", _config.GerdaAI.ComplexityEstimation?.IsEnabled ?? false },
            { "Ranking", _config.GerdaAI.Ranking?.IsEnabled ?? false },
            { "Dispatching", _config.GerdaAI.Dispatching?.IsEnabled ?? false },
            { "Anticipation", _config.GerdaAI.Anticipation?.IsEnabled ?? false }
        };

        return Task.FromResult(HealthCheckResult.Healthy(
            "GERDA AI services operational",
            data));
    }
}

/// <summary>
/// Health check for email ingestion service.
/// </summary>
public class EmailIngestionHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public EmailIngestionHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var imapServer = _configuration["EmailIngestion:ImapServer"];
        var enabled = !string.IsNullOrEmpty(imapServer);

        if (!enabled)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "Email ingestion not configured"));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            "Email ingestion configured",
            new Dictionary<string, object>
            {
                { "ImapServer", imapServer ?? "N/A" }
            }));
    }
}

/// <summary>
/// Health check for background task queue.
/// </summary>
public class BackgroundQueueHealthCheck : IHealthCheck
{
    private readonly IBackgroundTaskQueue _queue;

    public BackgroundQueueHealthCheck(IBackgroundTaskQueue queue)
    {
        _queue = queue;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var count = _queue.QueuedCount;
        var data = new Dictionary<string, object>
        {
            { "QueuedCount", count }
        };

        if (count > 500)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Background queue backlog high ({count} items)", null, data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            "Background task queue operational", data));
    }
}
