using TicketMasala.Web.Data;
using TicketMasala.Web.Data.Seeding;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Engine.Ingestion.Background;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.GERDA.Tickets.Domain;

namespace TicketMasala.Web.Extensions;

/// <summary>
/// Extension methods for configuring core business services.
/// </summary>
public static class CoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds core business services (Tickets, Projects, Notifications, etc.).
    /// </summary>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // Custom Field Validation
        services.AddScoped<Engine.Ingestion.Validation.ICustomFieldValidationService,
            Engine.Ingestion.Validation.CustomFieldValidationService>();

        // Rule Engine
        services.AddScoped<Engine.Compiler.IRuleEngineService,
            Engine.Compiler.RuleEngineService>();

        // Metrics
        services.AddScoped<IMetricsService, MetricsService>();

        // PII Scrubber
        services.AddScoped<Engine.Security.IPiiScrubberService,
            Engine.Security.PiiScrubberService>();

        // Ticket Services (Split by Responsibility)
        services.AddScoped<ITicketReadService, TicketReadService>(); // Read/Search
        services.AddScoped<ITicketWorkflowService, TicketWorkflowService>(); // Workflow
        services.AddScoped<ITicketBatchService, TicketBatchService>(); // Batch Operations
        services.AddScoped<TicketService>(); // Legacy implementation

        // Domain services for TicketService dependencies
        services.AddScoped<TicketDispatchService>();
        services.AddScoped<TicketReportingService>();
        services.AddScoped<TicketNotificationService>();

        // Factory Pattern for Ticket creation
        services.AddScoped<ITicketFactory, TicketFactory>();

        // Other services
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ISavedFilterService, SavedFilterService>();
        services.AddScoped<IProjectReadService, ProjectReadService>();
        services.AddScoped<IProjectWorkflowService, ProjectWorkflowService>();
        services.AddScoped<IProjectTemplateService, ProjectTemplateService>();
        services.AddScoped<ProjectService>(); // Legacy
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ITicketImportService, TicketImportService>();

        // Seed Strategies (Strategy Pattern - executed in registration order)
        services.AddScoped<ISeedStrategy, RoleSeedStrategy>();
        services.AddScoped<ISeedStrategy, UserSeedStrategy>();
        services.AddScoped<ISeedStrategy, ProjectSeedStrategy>();
        services.AddScoped<ISeedStrategy, KnowledgeBaseSeedStrategy>();

        // DbSeeder (orchestrator)
        services.AddScoped<DbSeeder>();

        // Ingestion
        services.AddScoped<IEmailTicketProcessor, EmailTicketProcessor>();

        return services;
    }

    /// <summary>
    /// Adds background services (Email Ingestion, Ticket Generator, Task Queue).
    /// </summary>
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        // Background Queue
        services.AddSingleton<IBackgroundTaskQueue>(ctx => new BackgroundQueue(100));
        services.AddSingleton(System.Threading.Channels.Channel.CreateUnbounded<ViewModels.Ingestion.IngestionWorkItem>());

        // Enrichment Queue
        services.AddSingleton<Engine.Enrichment.IEnrichmentQueue, Engine.Enrichment.EnrichmentQueue>();

        // Hosted Services
        services.AddHostedService<EmailIngestionService>();
        services.AddHostedService<QueuedHostedService>();
        services.AddHostedService<TicketGeneratorService>();
        services.AddHostedService<Engine.Enrichment.EnrichmentBackgroundService>();

        // Ticket Generator
        services.AddScoped<ITicketGenerator, TicketGenerator>();

        // Import Dispatcher
        services.AddSingleton<ITicketImportDispatcher, TicketImportDispatcher>();

        return services;
    }
}
