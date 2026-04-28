using TicketMasala.Web.Observers;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Engine.Projects;

/// <summary>
/// Extension methods to register all Project module services.
/// Includes project repositories, services, observers, and seeding.
/// </summary>
public static class ProjectServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Project module services to the dependency injection container.
    /// Includes repository, read/workflow/template services, and observers.
    /// </summary>
    public static IServiceCollection AddProjectModule(this IServiceCollection services)
    {
        // ============================================
        // Register Repository
        // ============================================
        services.AddScoped<IProjectRepository, EfCoreProjectRepository>();

        // ============================================
        // Register Project Services (CQRS Pattern)
        // ============================================
        services.AddScoped<IProjectReadService, ProjectReadService>();
        services.AddScoped<IProjectWorkflowService, ProjectWorkflowService>();
        services.AddScoped<IProjectTemplateService, ProjectTemplateService>();

        // ============================================
        // Register Project Observers (Observer Pattern)
        // ============================================
        services.AddScoped<IProjectObserver, LoggingProjectObserver>();
        services.AddScoped<IProjectObserver, NotificationProjectObserver>();

        return services;
    }
}
