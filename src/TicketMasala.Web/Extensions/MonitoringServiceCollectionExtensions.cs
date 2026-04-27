using Microsoft.Extensions.Diagnostics.HealthChecks;
using TicketMasala.Web.Health;

namespace TicketMasala.Web.Extensions;

public static class MonitoringServiceCollectionExtensions
{
    public static IServiceCollection AddMasalaMonitoring(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<GerdaHealthCheck>("gerda-ai")
            .AddCheck<EmailIngestionHealthCheck>("email-ingestion")
            .AddCheck<BackgroundQueueHealthCheck>("background-queue");

        return services;
    }
}
