using TicketMasala.Web.Observers;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Extensions;

/// <summary>
/// Extension methods for configuring repository and observer services.
/// </summary>
public static class RepositoryServiceCollectionExtensions
{
    /// <summary>
    /// Adds repository pattern implementations.
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ITicketRepository, EfCoreTicketRepository>();
        services.AddScoped<IProjectRepository, EfCoreProjectRepository>();
        services.AddScoped<IUserRepository, EfCoreUserRepository>();
        services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();

        return services;
    }

    /// <summary>
    /// Adds observer pattern implementations for tickets, projects, and comments.
    /// </summary>
    public static IServiceCollection AddObservers(this IServiceCollection services)
    {
        // Ticket Observers
        services.AddScoped<ITicketObserver, GerdaTicketObserver>();
        services.AddScoped<ITicketObserver, LoggingTicketObserver>();
        services.AddScoped<ITicketObserver, NotificationTicketObserver>();

        // Project Observers
        services.AddScoped<IProjectObserver, LoggingProjectObserver>();
        services.AddScoped<IProjectObserver, NotificationProjectObserver>();

        // Comment Observers
        services.AddScoped<ICommentObserver, LoggingCommentObserver>();
        services.AddScoped<ICommentObserver, NotificationCommentObserver>();

        return services;
    }
}
