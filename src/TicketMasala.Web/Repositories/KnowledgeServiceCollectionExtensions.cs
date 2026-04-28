namespace TicketMasala.Web.Repositories;

/// <summary>
/// Extension methods to register all Knowledge Base module services.
/// Includes Knowledge Base and Knowledge Snippet repositories.
/// Note: GERDA Knowledge services are registered separately via AddGerda().
/// </summary>
public static class KnowledgeServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Knowledge Base module services to the dependency injection container.
    /// Includes repositories for knowledge base articles and snippets.
    /// </summary>
    public static IServiceCollection AddKnowledgeModule(this IServiceCollection services)
    {
        // ============================================
        // Register Knowledge Repositories
        // ============================================
        services.AddScoped<IKnowledgeBaseRepository, EfCoreKnowledgeBaseRepository>();
        services.AddScoped<IKnowledgeSnippetRepository, EfCoreKnowledgeSnippetRepository>();

        return services;
    }
}
