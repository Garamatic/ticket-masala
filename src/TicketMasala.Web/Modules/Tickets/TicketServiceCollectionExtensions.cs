using TicketMasala.Domain.Events;
using TicketMasala.Web.Engine.GERDA.BackgroundJobs;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.GERDA.Tickets.Domain;
using TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Handlers.DomainEvents;
using TicketMasala.Web.Infrastructure.DomainEvents;
using TicketMasala.Web.Modules.Tickets.Internal;
using TicketMasala.Web.Observers;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Modules.Tickets;

/// <summary>
/// Extension methods to register all Ticket module services.
/// Centralizes ticket-related DI registrations for better modularity.
/// </summary>
public static class TicketServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Ticket module services to the dependency injection container.
    /// Includes repositories, domain services, ticket-specific services, and observers.
    /// </summary>
    public static IServiceCollection AddTicketModule(this IServiceCollection services)
    {
        // ============================================
        // Register Repositories (Repository Pattern)
        // ============================================
        services.AddScoped<ITicketRepository, EfCoreTicketRepository>();
        services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();

        // ============================================
        // Register Domain Services (Phase 4: Rich Domain Model)
        // ============================================
        services.AddScoped<ITicketAssignmentService, TicketAssignmentService>();
        services.AddScoped<ITicketGroupingService, TicketGroupingService>();

        // ============================================
        // Register Deep Modules (Ticket Module Internals)
        // ============================================
        services.AddScoped<ITicketModule, TicketModule>();
        services.AddScoped<ITicketLifecycleService, TicketLifecycleService>();
        services.AddScoped<ITicketQueryService, TicketQueryService>();
        services.AddScoped<ITicketAuthorizationService, TicketAuthorizationService>();

        // ============================================
        // Register Ticket Observers (Observer Pattern)
        // ============================================
        services.AddScoped<ITicketObserver, GerdaTicketObserver>();
        services.AddScoped<ITicketObserver, LoggingTicketObserver>();
        services.AddScoped<ITicketObserver, NotificationTicketObserver>();

        // ============================================
        // Register Domain Event Handlers
        // ============================================
        services.AddScoped<IDomainEventHandler<TicketCreatedEvent>, TicketCreatedGerdaHandler>();

        // ============================================
        // Register Ticket Services (CQRS + Responsibility Split)
        // ============================================
        // Read/Query Services
        services.AddScoped<ITicketReadService, TicketReadService>();
        services.AddScoped<ITicketDetailService, TicketDetailService>();

        // Write/Command Services
        services.AddScoped<ITicketCreateService, TicketCreateService>();
        services.AddScoped<ITicketEditService, TicketEditService>();
        services.AddScoped<ITicketBatchService, TicketBatchService>();

        // Deep Module: ITicketLifecycle (replaces all shallow workflow services below)
        services.AddTicketLifecycle();

        // [OBSOLETE] Shallow workflow services — kept for reference during migration.
        // Will be removed in a future release. All new code should use ITicketLifecycle.
        // TODO: Remove these registrations once confirm no runtime regressions.
        // services.AddScoped<ITicketWorkflowService, TicketWorkflowService>();
        // services.AddScoped<ITicketResolutionService, TicketResolutionService>();
        // services.AddScoped<ITicketCommentService, TicketCommentService>();
        // services.AddScoped<ITicketReviewService, TicketReviewService>();
        // services.AddScoped<ITicketTimeLoggingService, TicketTimeLoggingService>();
        // services.AddScoped<ITicketCreationService, TicketCreationService>();
        // services.AddScoped<ITicketUpdateService, TicketUpdateService>();
        // services.AddScoped<ITicketAssignmentFacade, TicketAssignmentFacade>();

        // Specialized Services
        services.AddScoped<TicketDispatchService>();
        services.AddScoped<TicketReportingService>();
        services.AddScoped<TicketNotificationService>();
        services.AddScoped<ITicketFactory, TicketFactory>();
        services.AddScoped<ITicketImportService, TicketImportService>();

        // ============================================
        // Register Ticket Generator (Background Service)
        // ============================================
        services.AddHostedService<TicketGeneratorService>();
        services.AddScoped<ITicketGenerator, TicketGenerator>();

        return services;
    }
}
