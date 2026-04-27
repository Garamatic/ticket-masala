using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ML;
using TicketMasala.Web.Engine.GERDA;
using TicketMasala.Web.Engine.GERDA.Anticipation;
using TicketMasala.Web.Engine.GERDA.Dispatching;
using TicketMasala.Web.Engine.GERDA.Estimating;
using TicketMasala.Web.Engine.GERDA.Grouping;
using TicketMasala.Web.Engine.GERDA.Knowledge;
using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Engine.GERDA.Ranking;
using TicketMasala.Web.Engine.GERDA.Strategies;
using TicketMasala.Web.Engine.GERDA.Tickets;

namespace TicketMasala.Web.Extensions;

/// <summary>
/// Extension methods for registering the deepened GERDA module.
/// </summary>
public static class GerdaServiceExtensions
{
    /// <summary>
    /// Adds the GERDA AI module as a deep service.
    /// Single registration call replaces 15+ individual service registrations.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Configuration options</param>
    public static IServiceCollection AddGerda(
        this IServiceCollection services,
        Action<GerdaOptions>? configureOptions = null)
    {
        var options = new GerdaOptions();
        configureOptions?.Invoke(options);

        // Register options for internal use
        services.AddSingleton(options);

        // Load configuration
        var config = LoadGerdaConfig(options);
        services.AddSingleton(config);

        // If GERDA is disabled, register no-op and return early
        if (!config.GerdaAI.IsEnabled)
        {
            services.AddSingleton<IGerda, NoOpGerda>();
            return services;
        }

        // Register internal stage engines (hidden from callers)
        RegisterStageEngines(services, config);

        // Register the deep module (the only public interface)
        services.AddScoped<IGerda, GerdaEngine>();

        // Register legacy services for backward compatibility during migration
        // These will be removed in a future release once all callers migrate to IGerda
        RegisterLegacyServices(services, config, options);

        // Register background service if enabled
        if (config.GerdaAI.Anticipation.IsEnabled)
        {
            services.AddHostedService<GerdaMaintenanceService>();
        }

        return services;
    }

    /// <summary>
    /// Logs GERDA configuration status.
    /// Called after AddGerda to provide visibility into configuration.
    /// </summary>
    public static IServiceCollection LogGerdaConfiguration(
        this IServiceCollection services,
        ILogger logger)
    {
        // Note: This method runs during service registration, before the service provider is built.
        // The actual validation happens at runtime when services are resolved.
        logger.LogInformation("GERDA registration complete. Configuration will be validated at runtime.");
        return services;
    }

    private static GerdaConfig LoadGerdaConfig(GerdaOptions options)
    {
        var configPath = Path.Combine(options.ConfigBasePath ?? "", "masala_config.json");

        if (!File.Exists(configPath))
        {
            // Return default (disabled) config if file doesn't exist
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

    private static void RegisterStageEngines(IServiceCollection services, GerdaConfig config)
    {
        // Grouping (G)
        if (config.GerdaAI.SpamDetection.IsEnabled)
        {
            services.AddScoped<IGroupingEngine, GroupingEngine>();
        }
        else
        {
            services.AddSingleton<IGroupingEngine, NoOpGroupingEngine>();
        }

        // Estimating (E)
        if (config.GerdaAI.ComplexityEstimation.IsEnabled)
        {
            services.AddScoped<IEstimatingEngine, EstimatingEngine>();
            RegisterEstimatingStrategies(services);
        }
        else
        {
            services.AddSingleton<IEstimatingEngine, NoOpEstimatingEngine>();
        }

        // Ranking (R)
        if (config.GerdaAI.Ranking.IsEnabled)
        {
            services.AddScoped<IRankingEngine, RankingEngine>();
            RegisterRankingStrategies(services);
        }
        else
        {
            services.AddSingleton<IRankingEngine, NoOpRankingEngine>();
        }

        // Dispatching (D)
        if (config.GerdaAI.Dispatching.IsEnabled)
        {
            services.AddScoped<IDispatchingEngine, DispatchingEngine>();
            // Strategies are registered in RegisterLegacyServices with proper options
        }
        else
        {
            services.AddSingleton<IDispatchingEngine, NoOpDispatchingEngine>();
        }

        // Knowledge (K)
        if (config.GerdaAI.Knowledge.IsEnabled)
        {
            services.AddScoped<IKnowledgeEngine, KnowledgeEngine>();
        }
        else
        {
            services.AddSingleton<IKnowledgeEngine, NoOpKnowledgeEngine>();
        }

        // Anticipation (A)
        if (config.GerdaAI.Anticipation.IsEnabled)
        {
            services.AddScoped<IAnticipationEngine, AnticipationEngine>();
        }
    }

    private static void RegisterEstimatingStrategies(IServiceCollection services)
    {
        services.AddScoped<IEstimatingStrategy, CategoryBasedEstimatingStrategy>();
        services.AddScoped<IEstimatingStrategy, GardenComplexityStrategy>();
    }

    private static void RegisterRankingStrategies(IServiceCollection services)
    {
        services.AddScoped<IJobRankingStrategy, WeightedShortestJobFirstStrategy>();
        services.AddScoped<IJobRankingStrategy, SeasonalPriorityStrategy>();
    }

    private static void RegisterDispatchingStrategies(IServiceCollection services, GerdaOptions options)
    {
        var modelPath = Path.Combine(options.ModelPath ?? "", "gerda_dispatch_model.zip");

        // Only register MatrixFactorization if model file exists or not using mock predictions
        if (File.Exists(modelPath) && !options.UseMockMlPredictions)
        {
            services.AddScoped<IDispatchingStrategy, MatrixFactorizationDispatchingStrategy>();
        }

        // Always register ZoneBased as fallback
        services.AddScoped<IDispatchingStrategy, ZoneBasedDispatchingStrategy>();
    }

    /// <summary>
    /// Registers legacy GERDA services for backward compatibility.
    /// These allow existing code to continue working while migrating to IGerda.
    /// </summary>
    private static void RegisterLegacyServices(IServiceCollection services, GerdaConfig config, GerdaOptions options)
    {
        // Register strategy factory (required by legacy services)
        services.TryAddScoped<TicketMasala.Web.Engine.GERDA.Strategies.IStrategyFactory,
            TicketMasala.Web.Engine.GERDA.Strategies.StrategyFactory>();

        // Register WSJF Engine dependencies (required by RankingService)
        services.AddSingleton(new TicketMasala.Web.Engine.GERDA.Dispatching.Configuration.WsjfConfig());
        services.AddTransient<TicketMasala.Web.Engine.GERDA.Dispatching.Algorithms.WsjfEngine>();

        // Register feature extractor if dispatching is enabled
        if (config.GerdaAI.Dispatching.IsEnabled)
        {
            services.AddScoped<TicketMasala.Web.Engine.GERDA.Features.IFeatureExtractor,
                TicketMasala.Web.Engine.GERDA.Features.DynamicFeatureExtractor>();
        }

        // Legacy service registrations for backward compatibility
        if (config.GerdaAI.SpamDetection.IsEnabled)
        {
            services.AddScoped<TicketMasala.Web.Engine.GERDA.Grouping.IGroupingService,
                TicketMasala.Web.Engine.GERDA.Grouping.GroupingService>();
        }

        if (config.GerdaAI.ComplexityEstimation.IsEnabled)
        {
            services.AddScoped<TicketMasala.Web.Engine.GERDA.Estimating.IEstimatingService,
                TicketMasala.Web.Engine.GERDA.Estimating.EstimatingService>();
        }

        if (config.GerdaAI.Ranking.IsEnabled)
        {
            services.AddScoped<TicketMasala.Web.Engine.GERDA.Ranking.IRankingService,
                TicketMasala.Web.Engine.GERDA.Ranking.RankingService>();
        }

        if (config.GerdaAI.Dispatching.IsEnabled)
        {
            // DISPATCHING ARCHITECTURE (Issue #7: Consolidated Implementation)
            // Single consolidated path using AgentMatchingEngine + IAffinityScorer plugins

            // Core dispatching configuration
            services.AddScoped<TicketMasala.Web.Engine.GERDA.Dispatching.Configuration.DispatchingConfig>(sp =>
            {
                var gerdaConfig = sp.GetRequiredService<GerdaConfig>();
                return new TicketMasala.Web.Engine.GERDA.Dispatching.Configuration.DispatchingConfig
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

            // ML.NET Prediction Engine Pool and IAffinityScorer (only if model file exists)
            var modelPath = Path.Combine(options.ModelPath ?? "", "gerda_dispatch_model.zip");
            if (File.Exists(modelPath) && !options.UseMockMlPredictions)
            {
                services.AddPredictionEnginePool<TicketMasala.Web.Engine.GERDA.Models.AgentCustomerRating,
                        TicketMasala.Web.Engine.GERDA.Models.RatingPrediction>()
                    .FromFile(modelName: "GerdaDispatchModel", filePath: modelPath, watchForChanges: true);

                // IAffinityScorer plugin (Matrix Factorization ML-based affinity scoring)
                services.AddScoped<TicketMasala.Web.Engine.GERDA.Dispatching.IAffinityScorer,
                    TicketMasala.Web.Engine.GERDA.Dispatching.MatrixFactorizationAffinityScorer>();
            }
            else
            {
                // No-op affinity scorer when model is not available
                services.AddSingleton<TicketMasala.Web.Engine.GERDA.Dispatching.IAffinityScorer,
                    TicketMasala.Web.Engine.GERDA.Dispatching.NoOpAffinityScorer>();
            }

            // Register dispatching strategies (for legacy shim compatibility)
            RegisterDispatchingStrategies(services, options);

            // Core Agent Matching Engine (with optional IAffinityScorer injected)
            services.AddScoped<TicketMasala.Web.Engine.GERDA.Dispatching.Algorithms.AgentMatchingEngine>(sp =>
            {
                var dispatchConfig = sp.GetRequiredService<TicketMasala.Web.Engine.GERDA.Dispatching.Configuration.DispatchingConfig>();
                var logger = sp.GetRequiredService<ILogger<TicketMasala.Web.Engine.GERDA.Dispatching.Algorithms.AgentMatchingEngine>>();
                var affinityScorer = sp.GetService<TicketMasala.Web.Engine.GERDA.Dispatching.IAffinityScorer>(); // Optional
                return new TicketMasala.Web.Engine.GERDA.Dispatching.Algorithms.AgentMatchingEngine(dispatchConfig, logger, affinityScorer);
            });

            // CONSOLIDATED DISPATCHING SERVICE (Issue #7)
            services.AddScoped<TicketMasala.Web.Engine.GERDA.Dispatching.IDispatchingService,
                TicketMasala.Web.Engine.GERDA.Dispatching.DispatchingService>();
            services.AddScoped<TicketMasala.Web.Engine.GERDA.Tickets.IDispatchBacklogService,
                TicketMasala.Web.Engine.GERDA.Tickets.DispatchBacklogService>();
            services.AddScoped<TicketMasala.Web.Engine.GERDA.Dispatching.IDispatchingStrategySelector,
                TicketMasala.Web.Engine.GERDA.Dispatching.DomainDispatchingStrategySelector>();
            services.AddScoped<TicketMasala.Web.Engine.GERDA.Dispatching.IAutoDispatchPolicy,
                TicketMasala.Web.Engine.GERDA.Dispatching.ScoreThresholdAutoDispatchPolicy>();
            services.AddScoped<TicketMasala.Web.Engine.GERDA.Dispatching.IProjectManagerRecommendationService,
                TicketMasala.Web.Engine.GERDA.Dispatching.WorkloadAndSuccessProjectManagerRecommendationService>();
        }
        else
        {
            services.AddScoped<TicketMasala.Web.Engine.GERDA.Dispatching.IDispatchingService,
                TicketMasala.Web.Engine.GERDA.Dispatching.NoOpDispatchingService>();
        }

        if (config.GerdaAI.Knowledge.IsEnabled)
        {
            services.AddScoped<TicketMasala.Web.Engine.GERDA.Knowledge.IKnowledgeService,
                TicketMasala.Web.Engine.GERDA.Knowledge.KnowledgeService>();
        }
        else
        {
            services.AddScoped<TicketMasala.Web.Engine.GERDA.Knowledge.IKnowledgeService,
                TicketMasala.Web.Engine.GERDA.Knowledge.NoOpKnowledgeService>();
        }

        if (config.GerdaAI.Anticipation.IsEnabled)
        {
            services.AddScoped<TicketMasala.Web.Engine.GERDA.Anticipation.IAnticipationService,
                TicketMasala.Web.Engine.GERDA.Anticipation.AnticipationService>();
        }

        // Legacy IGerdaService registration (redirects to new IGerda)
        services.AddScoped<TicketMasala.Web.Engine.GERDA.IGerdaService>(sp =>
            new GerdaServiceAdapter(sp.GetRequiredService<IGerda>()));
    }
}

/// <summary>
/// Adapter to make new IGerda compatible with legacy IGerdaService interface.
/// </summary>
internal sealed class GerdaServiceAdapter : TicketMasala.Web.Engine.GERDA.IGerdaService
{
    private readonly IGerda _gerda;

    public GerdaServiceAdapter(IGerda gerda)
    {
        _gerda = gerda;
    }

    public bool IsEnabled => _gerda.IsActive;

    public async Task ProcessTicketAsync(Guid ticketGuid)
    {
        await _gerda.ProcessAsync(ticketGuid);
    }

    public async Task ProcessAllOpenTicketsAsync()
    {
        // This method is no longer supported via the deep interface
        // It should be called through the maintenance service directly
        throw new NotSupportedException(
            "ProcessAllOpenTicketsAsync is deprecated. Use GerdaMaintenanceService or IGerda for single ticket processing.");
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
    /// If true, uses mock ML predictions for tests (skips model file loading).
    /// </summary>
    public bool UseMockMlPredictions { get; set; }

    /// <summary>
    /// If false, disables file watcher for config reload (use in tests).
    /// </summary>
    public bool EnableConfigReload { get; set; } = true;
}
