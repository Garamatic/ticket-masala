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

        // Ticket Service (implements ITicketService, ITicketQueryService, ITicketCommandService)
        services.AddScoped<TicketService>();
        services.AddScoped<ITicketService>(sp => sp.GetRequiredService<TicketService>());
        services.AddScoped<ITicketQueryService>(sp => sp.GetRequiredService<TicketService>());
        services.AddScoped<ITicketCommandService>(sp => sp.GetRequiredService<TicketService>());

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
        services.AddScoped<IProjectService, ProjectService>();
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

        // Hosted Services
        services.AddHostedService<EmailIngestionService>();
        services.AddHostedService<QueuedHostedService>();
        services.AddHostedService<TicketGeneratorService>();

        // Ticket Generator
        services.AddScoped<ITicketGenerator, TicketGenerator>();

        return services;
    }
}
