using TicketMasala.Web.Engine.Compiler;
using TicketMasala.Web.Engine.Ingestion.Background;
using TicketMasala.Web.Engine.Ingestion.Validation;

namespace TicketMasala.Web.Engine.Ingestion;

/// <summary>
/// Extension methods to register all Ingestion module services.
/// Includes email processing, background queue, validation, and hosted services.
/// </summary>
public static class IngestionServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Ingestion module services to the dependency injection container.
    /// Includes email ticket processing, background task queue, validation, and hosted services.
    /// </summary>
    public static IServiceCollection AddIngestionModule(this IServiceCollection services)
    {
        // ============================================
        // Register Email Processing
        // ============================================
        services.AddScoped<IEmailTicketProcessor, EmailTicketProcessor>();
        services.AddHostedService<EmailIngestionService>();

        // ============================================
        // Register Background Task Queue Infrastructure
        // ============================================
        services.AddSingleton<IBackgroundTaskQueue>(ctx => new BackgroundQueue(100));
        services.AddSingleton(System.Threading.Channels.Channel.CreateUnbounded<TicketMasala.Web.ViewModels.Ingestion.IngestionWorkItem>());
        services.AddHostedService<QueuedHostedService>();

        // ============================================
        // Register Validation & Rule Engine Services
        // ============================================
        services.AddScoped<ICustomFieldValidationService, CustomFieldValidationService>();
        services.AddSingleton<RuleCompilerService>();
        services.AddScoped<IRuleEngineService, RuleEngineService>();

        return services;
    }
}
