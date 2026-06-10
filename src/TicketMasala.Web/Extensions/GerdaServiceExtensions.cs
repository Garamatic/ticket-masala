using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ML;
using TicketMasala.Web.Engine.GERDA;
using TicketMasala.Web.Engine.GERDA.Anticipation;
using TicketMasala.Web.Engine.GERDA.Dispatching;
using TicketMasala.Web.Engine.GERDA.Dispatching.Algorithms;
using TicketMasala.Web.Engine.GERDA.Dispatching.Configuration;
using TicketMasala.Web.Engine.GERDA.Estimating;
using TicketMasala.Web.Engine.GERDA.Features;
using TicketMasala.Web.Engine.GERDA.Grouping;
using TicketMasala.Web.Engine.GERDA.Knowledge;
using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Engine.GERDA.Ranking;
using TicketMasala.Web.Engine.GERDA.Strategies;
using TicketMasala.Web.Engine.GERDA.Tickets;

namespace TicketMasala.Web.Extensions;

/// <summary>
/// Extension methods for registering the deepened GERDA module.
/// Entry point that delegates to focused registration methods.
/// </summary>
public static class GerdaServiceExtensions
{
    /// <summary>
    /// Adds the GERDA AI module as a deep service.
    /// Single registration call composes focused registration methods.
    /// </summary>
    public static IServiceCollection AddGerda(
        this IServiceCollection services,
        Action<GerdaOptions>? configureOptions = null)
    {
        var options = new GerdaOptions();
        configureOptions?.Invoke(options);

        services.AddSingleton(options);

        // Load and register configuration
        var config = GerdaConfigurationLoader.Load(options);
        services.AddSingleton(config);

        // Early exit if GERDA is disabled
        if (!config.GerdaAI.IsEnabled)
        {
            services.AddSingleton<IGerda, NoOpGerda>();
            services.TryAddScoped<ITicketDispatcher, NoOpDispatcher>();
            services.TryAddScoped<IEstimatingService, NoOpEstimatingService>();
            services.TryAddScoped<IKnowledgeService, NoOpKnowledgeService>();
            return services;
        }

        // Register stage engines (the core GERDA implementation)
        GerdaStageEngineRegistration.Register(services, config);

        // Register composable execution stages and provider used by GerdaEngine.
        GerdaStageProviderRegistration.Register(services, config);

        // Register the deep module facade (primary public interface)
        services.AddScoped<IGerda, GerdaEngine>();

        // Register legacy services for backward compatibility (removal target: 30 days)
        GerdaLegacyServiceRegistration.Register(services, config, options);

        // Register background service if anticipation is enabled
        if (config.GerdaAI.Anticipation.IsEnabled)
        {
            services.AddHostedService<GerdaMaintenanceService>();
        }

        // Fallback registrations for dependencies needed by other components
        services.TryAddScoped<ITicketDispatcher, NoOpDispatcher>();
        services.TryAddScoped<IEstimatingService, NoOpEstimatingService>();
        services.TryAddScoped<IKnowledgeService, NoOpKnowledgeService>();

        return services;
    }

    /// <summary>
    /// Logs GERDA configuration status.
    /// </summary>
    public static IServiceCollection LogGerdaConfiguration(
        this IServiceCollection services,
        ILogger logger)
    {
        logger.LogInformation("GERDA registration complete. Configuration will be validated at runtime.");
        return services;
    }
}

/// <summary>
/// GERDA execution stage registrations for composable stage provider pattern.
/// </summary>
internal static class GerdaStageProviderRegistration
{
    public static void Register(IServiceCollection services, GerdaConfig config)
    {
        services.AddScoped<IGerdaExecutionStage, GroupingExecutionStage>();
        services.AddScoped<IGerdaExecutionStage, EstimatingExecutionStage>();
        services.AddScoped<IGerdaExecutionStage, RankingExecutionStage>();
        services.AddScoped<IGerdaExecutionStage, DispatchingExecutionStage>();
        services.AddScoped<IGerdaExecutionStage, KnowledgeExecutionStage>();

        if (config.GerdaAI.Anticipation.IsEnabled)
        {
            services.AddScoped<IGerdaExecutionStage, AnticipationExecutionStage>();
        }

        services.AddScoped<IGerdaStageProvider, DefaultGerdaStageProvider>();
    }
}

/// <summary>
/// Configuration options for GERDA registration.
/// </summary>
public class GerdaOptions
{
    /// <summary>
    /// Base path for configuration files (masala_config.json).
    /// </summary>
    public string? ConfigBasePath { get; set; }

    /// <summary>
    /// Path for ML.NET model files.
    /// </summary>
    public string? ModelPath { get; set; }

    /// <summary>
    /// If false, disables file watcher for config reload (use in tests).
    /// </summary>
    public bool EnableConfigReload { get; set; } = true;
}

/// <summary>
/// Configuration loading for GERDA.
/// </summary>
internal static class GerdaConfigurationLoader
{
    public static GerdaConfig Load(GerdaOptions options)
    {
        var configPath = Path.Combine(options.ConfigBasePath ?? "", "masala_config.json");

        if (!File.Exists(configPath))
        {
            return new GerdaConfig
            {
                GerdaAI = new GerdaAISettings { IsEnabled = false }
            };
        }

        var json = File.ReadAllText(configPath);
        var config = System.Text.Json.JsonSerializer.Deserialize<GerdaConfig>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return config ?? new GerdaConfig { GerdaAI = new GerdaAISettings { IsEnabled = false } };
    }
}

/// <summary>
/// Stage engine registrations (GERDA's core G.E.R.D.K.A engines).
/// </summary>
internal static class GerdaStageEngineRegistration
{
    public static void Register(IServiceCollection services, GerdaConfig config)
    {
        // Grouping (G)
        if (config.GerdaAI.SpamDetection.IsEnabled)
            services.AddScoped<IGroupingEngine, GroupingEngine>();
        else
            services.AddSingleton<IGroupingEngine, NoOpGroupingEngine>();

        // Estimating (E)
        if (config.GerdaAI.ComplexityEstimation.IsEnabled)
        {
            services.AddScoped<IEstimatingEngine, EstimatingEngine>();
            services.AddScoped<IEstimatingStrategy, CategoryBasedEstimatingStrategy>();
            services.AddScoped<IEstimatingStrategy, GardenComplexityStrategy>();
        }
        else
        {
            services.AddSingleton<IEstimatingEngine, NoOpEstimatingEngine>();
        }

        // Ranking (R)
        if (config.GerdaAI.Ranking.IsEnabled)
        {
            services.AddScoped<IRankingEngine, RankingEngine>();
            services.AddScoped<IJobRankingStrategy, WeightedShortestJobFirstStrategy>();
            services.AddScoped<IJobRankingStrategy, SeasonalPriorityStrategy>();
        }
        else
        {
            services.AddSingleton<IRankingEngine, NoOpRankingEngine>();
        }

        // Dispatching (D)
        if (config.GerdaAI.Dispatching.IsEnabled)
            services.AddScoped<IDispatchingEngine, DispatchingEngine>();
        else
            services.AddSingleton<IDispatchingEngine, NoOpDispatchingEngine>();

        // Knowledge (K)
        if (config.GerdaAI.Knowledge.IsEnabled)
            services.AddScoped<IKnowledgeEngine, KnowledgeEngine>();
        else
            services.AddSingleton<IKnowledgeEngine, NoOpKnowledgeEngine>();

        // Anticipation (A)
        if (config.GerdaAI.Anticipation.IsEnabled)
            services.AddScoped<IAnticipationEngine, AnticipationEngine>();
    }
}

/// <summary>
/// Legacy service registrations for backward compatibility.
/// REMOVAL TARGET: 30 days from migration start.
/// </summary>
internal static class GerdaLegacyServiceRegistration
{
    public static void Register(IServiceCollection services, GerdaConfig config, GerdaOptions options)
    {
        // Strategy factory (required by legacy services)
        services.TryAddScoped<IStrategyFactory, StrategyFactory>();

        // WSJF Engine dependencies (required by RankingService)
        services.AddSingleton(new WsjfConfig());
        services.AddTransient<WsjfEngine>();

        // Feature extractor (if dispatching enabled)
        if (config.GerdaAI.Dispatching.IsEnabled)
        {
            services.AddScoped<IFeatureExtractor, DynamicFeatureExtractor>();
        }

        // Individual feature services (legacy compatibility)
        RegisterGroupingService(services, config);
        RegisterEstimatingService(services, config);
        RegisterRankingService(services, config);
        RegisterDispatchingService(services, config, options);
        RegisterKnowledgeService(services, config);
        RegisterAnticipationService(services, config);
    }

    private static void RegisterGroupingService(IServiceCollection services, GerdaConfig config)
    {
        if (config.GerdaAI.SpamDetection.IsEnabled)
            services.AddScoped<IGroupingService, GroupingService>();
    }

    private static void RegisterEstimatingService(IServiceCollection services, GerdaConfig config)
    {
        if (config.GerdaAI.ComplexityEstimation.IsEnabled)
            services.AddScoped<IEstimatingService, EstimatingService>();
    }

    private static void RegisterRankingService(IServiceCollection services, GerdaConfig config)
    {
        if (config.GerdaAI.Ranking.IsEnabled)
            services.AddScoped<IRankingService, RankingService>();
    }

    private static void RegisterDispatchingService(IServiceCollection services, GerdaConfig config, GerdaOptions options)
    {
        // Dispatching configuration
        services.AddScoped<DispatchingConfig>(sp =>
        {
            var gerdaConfig = sp.GetRequiredService<GerdaConfig>();
            return new DispatchingConfig
            {
                MaxCasesPerAgent = gerdaConfig.GerdaAI.Dispatching.MaxAssignedTicketsPerAgent,
                ConfidenceThreshold = 70m,
                OptimalUtilizationThreshold = 0.6m,
                SkillMatchWeight = 0.35m,
                WorkloadBalanceWeight = 0.30m,
                AffinityWeight = 0.25m,
                AvailabilityWeight = 0.10m
            };
        });

        // ML.NET model and affinity scorer
        RegisterAffinityScorer(services, options);

        // Core dispatching components
        services.AddScoped<AgentMatchingEngine>(sp =>
        {
            var dispatchConfig = sp.GetRequiredService<DispatchingConfig>();
            var logger = sp.GetRequiredService<ILogger<AgentMatchingEngine>>();
            var affinityScorer = sp.GetService<IAffinityScorer>();
            return new AgentMatchingEngine(dispatchConfig, logger, affinityScorer);
        });

        // Deep module: ITicketDispatcher (replaces IDispatchingService)
        services.AddTicketDispatcher();

        // Supporting services
        services.AddScoped<IDispatchBacklogService, DispatchBacklogService>();
        services.AddScoped<IAutoDispatchPolicy, ScoreThresholdAutoDispatchPolicy>();
        services.AddScoped<IProjectManagerRecommendationService, WorkloadAndSuccessProjectManagerRecommendationService>();
    }

    private static void RegisterAffinityScorer(IServiceCollection services, GerdaOptions options)
    {
        var modelPath = Path.Combine(options.ModelPath ?? "", "gerda_dispatch_model.zip");

        if (File.Exists(modelPath))
        {
            services.AddPredictionEnginePool<AgentCustomerRating, RatingPrediction>()
                .FromFile(modelName: "GerdaDispatchModel", filePath: modelPath, watchForChanges: true);

            services.AddScoped<IAffinityScorer, MatrixFactorizationAffinityScorer>();
        }
        else
        {
            services.AddSingleton<IAffinityScorer, NoOpAffinityScorer>();
        }
    }

    private static void RegisterKnowledgeService(IServiceCollection services, GerdaConfig config)
    {
        if (config.GerdaAI.Knowledge.IsEnabled)
            services.AddScoped<IKnowledgeService, KnowledgeService>();
        else
            services.AddScoped<IKnowledgeService, NoOpKnowledgeService>();
    }

    private static void RegisterAnticipationService(IServiceCollection services, GerdaConfig config)
    {
        if (config.GerdaAI.Anticipation.IsEnabled)
            services.AddScoped<IAnticipationService, AnticipationService>();
    }
}
