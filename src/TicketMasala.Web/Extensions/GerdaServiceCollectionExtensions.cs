using Microsoft.Extensions.ML;
using TicketMasala.Web.Engine.Compiler;
using TicketMasala.Web.Engine.GERDA;
using TicketMasala.Web.Engine.GERDA.Anticipation;
using TicketMasala.Web.Engine.GERDA.BackgroundJobs;
using TicketMasala.Web.Engine.GERDA.Dispatching;
using TicketMasala.Web.Engine.GERDA.Estimating;
using TicketMasala.Web.Engine.GERDA.Grouping;
using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Engine.GERDA.Ranking;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Tenancy;

namespace TicketMasala.Web.Extensions;

/// <summary>
/// Extension methods for configuring GERDA AI services.
/// </summary>
public static class GerdaServiceCollectionExtensions
{
    /// <summary>
    /// Adds GERDA AI services (Grouping, Estimating, Ranking, Dispatching, Anticipation).
    /// </summary>
    public static IServiceCollection AddGerdaServices(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        string configBasePath)
    {
        var gerdaConfigPath = Path.Combine(configBasePath, "masala_config.json");
        Console.WriteLine($"Loading configuration from: {configBasePath}");

        // Domain Configuration Service (always available, uses defaults if no config file)
        services.AddSingleton<Engine.GERDA.Configuration.IDomainConfigurationService,
            Engine.GERDA.Configuration.DomainConfigurationService>();

        // Rule Compiler (global)
        services.AddSingleton<RuleCompilerService>();

        // Configuration Watcher
        services.AddHostedService<Engine.GERDA.Configuration.ConfigurationWatcherService>();

        if (!File.Exists(gerdaConfigPath))
        {
            Console.WriteLine($"Note: GERDA config not found at {gerdaConfigPath}");
            Console.WriteLine("Application will run with basic ticketing functionality");

            // Register NoOp services to prevent DI failures
            services.AddScoped<IDispatchingService, NoOpDispatchingService>();
            services.AddScoped<IGerdaService, NoOpGerdaService>();
            services.AddScoped<IEstimatingService, NoOpEstimatingService>();
            return services;
        }

        var gerdaConfigJson = File.ReadAllText(gerdaConfigPath);
        var gerdaConfig = System.Text.Json.JsonSerializer.Deserialize<GerdaConfig>(gerdaConfigJson,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (gerdaConfig == null)
        {
            services.AddScoped<IDispatchingService, NoOpDispatchingService>();
            services.AddScoped<IGerdaService, NoOpGerdaService>();
            return services;
        }

        services.AddSingleton(gerdaConfig);

        // Core GERDA services
        services.AddScoped<IGroupingService, GroupingService>();
        services.AddScoped<IEstimatingService, EstimatingService>();
        services.AddScoped<IRankingService, RankingService>();
        services.AddScoped<IDispatchingService, DispatchingService>();
        services.AddScoped<IDispatchBacklogService, DispatchBacklogService>();
        services.AddScoped<IAnticipationService, AnticipationService>();
        services.AddScoped<IGerdaService, GerdaService>();

        // Strategy Factory & Built-in Strategies
        services.AddScoped<Engine.GERDA.Strategies.IStrategyFactory, Engine.GERDA.Strategies.StrategyFactory>();
        services.AddScoped<IJobRankingStrategy, WeightedShortestJobFirstStrategy>();
        services.AddScoped<IJobRankingStrategy, SeasonalPriorityStrategy>();
        services.AddScoped<IEstimatingStrategy, CategoryBasedEstimatingStrategy>();
        services.AddScoped<IDispatchingStrategy, MatrixFactorizationDispatchingStrategy>();
        services.AddScoped<IDispatchingStrategy, ZoneBasedDispatchingStrategy>();

        // AI Features
        services.AddScoped<Engine.GERDA.Features.IFeatureExtractor, Engine.GERDA.Features.DynamicFeatureExtractor>();

        // ML.NET PredictionEnginePool for scalable inference
        var modelPath = Path.Combine(environment.ContentRootPath, "gerda_dispatch_model.zip");
        services.AddPredictionEnginePool<AgentCustomerRating, RatingPrediction>()
            .FromFile(modelName: "GerdaDispatchModel", filePath: modelPath, watchForChanges: true);

        // Background Service for automated maintenance
        services.AddHostedService<GerdaBackgroundService>();

        // Auto-register strategies from plugin assemblies
        StrategyAutoRegistration.RegisterPluginStrategies(services);

        Console.WriteLine("GERDA AI Services registered successfully (G+E+R+D+A + Background Jobs)");

        return services;
    }
}
