using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ML;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Data;
using TicketMasala.Web.Data.Seeding;
using TicketMasala.Web.Engine.Compiler;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA;
using TicketMasala.Web.Engine.GERDA.Anticipation;
using TicketMasala.Web.Engine.GERDA.BackgroundJobs;
using TicketMasala.Web.Engine.GERDA.Dispatching;
using TicketMasala.Web.Engine.GERDA.Dispatching.Algorithms;
using TicketMasala.Web.Engine.GERDA.Dispatching.Configuration;
using TicketMasala.Web.Engine.GERDA.Estimating;
using TicketMasala.Web.Engine.GERDA.Grouping;
using TicketMasala.Web.Engine.GERDA.Knowledge;
using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Engine.GERDA.Ranking;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.GERDA.Tickets.Domain;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Engine.Ingestion.Background;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.Health;
using TicketMasala.Web.Observers;
using TicketMasala.Web.Orchestrators;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Tenancy;
using WebOptimizer;

namespace TicketMasala.Web.Extensions;

/// <summary>
/// Extension methods to configure Ticket Masala core services.
/// Use these in your application's Program.cs to add all required services.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Adds all Ticket Masala core services to the application.
    /// Call this after registering any custom overrides (e.g., custom IJobRankingStrategy).
    /// </summary>
    public static WebApplicationBuilder AddMasalaCore(this WebApplicationBuilder builder)
    {
        // Identity configuration
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequiredUniqueChars = 2;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
        })
            .AddEntityFrameworkStores<MasalaDbContext>()
            .AddDefaultTokenProviders()
            .AddDefaultUI();

        // ============================================
        // Register Repositories (Repository Pattern)
        // ============================================
        builder.Services.AddScoped<ITicketRepository, EfCoreTicketRepository>();
        builder.Services.AddScoped<IProjectRepository, EfCoreProjectRepository>();
        builder.Services.AddScoped<IUserRepository, EfCoreUserRepository>();
        builder.Services.AddScoped<IKnowledgeBaseRepository, EfCoreKnowledgeBaseRepository>();
        builder.Services.AddScoped<IKnowledgeSnippetRepository, EfCoreKnowledgeSnippetRepository>();
        builder.Services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();

        // ============================================
        // Register Observers (Observer Pattern)
        // ============================================
        builder.Services.AddScoped<ITicketObserver, GerdaTicketObserver>();
        builder.Services.AddScoped<ITicketObserver, LoggingTicketObserver>();
        builder.Services.AddScoped<ITicketObserver, NotificationTicketObserver>();
        builder.Services.AddScoped<IProjectObserver, LoggingProjectObserver>();
        builder.Services.AddScoped<IProjectObserver, NotificationProjectObserver>();
        builder.Services.AddScoped<ICommentObserver, LoggingCommentObserver>();
        builder.Services.AddScoped<ICommentObserver, NotificationCommentObserver>();

        // ============================================
        // Register Services (CQRS + Factory Pattern)
        // ============================================
        // System abstractions for testability
        builder.Services.AddSingleton<TicketMasala.Web.Abstractions.ISystemClock, TicketMasala.Web.Services.SystemClock>();
        builder.Services.AddScoped<TicketMasala.Web.Services.IJsonParsingService, TicketMasala.Web.Services.JsonParsingService>();

        builder.Services.AddSingleton<RuleCompilerService>();
        builder.Services.AddScoped<TicketMasala.Web.Engine.Ingestion.Validation.ICustomFieldValidationService,
            TicketMasala.Web.Engine.Ingestion.Validation.CustomFieldValidationService>();
        builder.Services.AddScoped<IRuleEngineService, RuleEngineService>();
        builder.Services.AddScoped<TicketMasala.Web.Engine.Security.IPiiScrubberService,
            TicketMasala.Web.Engine.Security.PiiScrubberService>();
        builder.Services.AddScoped<IMetricsService, MetricsService>();

        // Ticket Services (Split by Responsibility)
        builder.Services.AddScoped<ITicketReadService, TicketReadService>();
        builder.Services.AddScoped<ITicketWorkflowService, TicketWorkflowService>();
        builder.Services.AddScoped<ITicketBatchService, TicketBatchService>();

        // Ticket View Services (Facade decomposition for SRP)
        builder.Services.AddScoped<ITicketDetailService, TicketDetailService>();
        builder.Services.AddScoped<ITicketCreateService, TicketCreateService>();
        builder.Services.AddScoped<ITicketEditService, TicketEditService>();

        // Orchestrators
        builder.Services.AddScoped<ITicketOrchestrator, TicketOrchestrator>();

        builder.Services.AddScoped<TicketDispatchService>();
        builder.Services.AddScoped<TicketReportingService>();
        builder.Services.AddScoped<TicketNotificationService>();
        builder.Services.AddScoped<ITicketFactory, TicketFactory>();
        builder.Services.AddScoped<IFileStorageService, DiskFileStorageService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddScoped<ISavedFilterService, SavedFilterService>();
        builder.Services.AddScoped<IProjectReadService, ProjectReadService>();
        builder.Services.AddScoped<IProjectWorkflowService, ProjectWorkflowService>();
        builder.Services.AddScoped<IProjectTemplateService, ProjectTemplateService>();
        builder.Services.AddScoped<IAuditService, AuditService>();
        builder.Services.AddScoped<ITicketImportService, TicketImportService>();

        // Ingestion & Sentiment
        builder.Services.AddScoped<IEmailTicketProcessor, EmailTicketProcessor>();
        builder.Services.AddScoped<TicketMasala.Web.Engine.GERDA.Sentiment.ISentimentAnalyzer,
            TicketMasala.Web.Engine.GERDA.Sentiment.SimpleSentimentAnalyzer>();

        builder.Services.AddHostedService<EmailIngestionService>();

        // Background Queue
        builder.Services.AddSingleton<IBackgroundTaskQueue>(ctx => new BackgroundQueue(100));
        builder.Services.AddSingleton(System.Threading.Channels.Channel.CreateUnbounded<TicketMasala.Web.ViewModels.Ingestion.IngestionWorkItem>());
        builder.Services.AddHostedService<QueuedHostedService>();
        builder.Services.AddHostedService<TicketGeneratorService>();
        builder.Services.AddScoped<ITicketGenerator, TicketGenerator>();

        // Enrichment
        builder.Services.AddSingleton<TicketMasala.Web.Engine.Enrichment.IEnrichmentQueue, TicketMasala.Web.Engine.Enrichment.EnrichmentQueue>();
        builder.Services.AddHostedService<TicketMasala.Web.Engine.Enrichment.EnrichmentBackgroundService>();

        // Seed Strategies (Strategy Pattern - executed in registration order)
        builder.Services.AddScoped<ISeedStrategy, RoleSeedStrategy>();
        builder.Services.AddScoped<ISeedStrategy, UserSeedStrategy>();
        builder.Services.AddScoped<ISeedStrategy, ProjectSeedStrategy>();
        builder.Services.AddScoped<ISeedStrategy, WorkItemSeedStrategy>();
        builder.Services.AddScoped<ISeedStrategy, KnowledgeBaseSeedStrategy>();

        // DbSeeder
        builder.Services.AddScoped<DbSeeder>();

        // ============================================
        // GERDA AI Services Configuration
        // ============================================
        var configBasePath = TicketMasala.Web.Configuration.ConfigurationPaths.GetConfigBasePath(builder.Environment.ContentRootPath);
        var gerdaConfigPath = Path.Combine(configBasePath, "masala_config.json");

        builder.Services.AddSingleton<TicketMasala.Web.Engine.GERDA.Configuration.IDomainConfigurationService,
            TicketMasala.Web.Engine.GERDA.Configuration.DomainConfigurationService>();
        builder.Services.AddScoped<TicketMasala.Web.Engine.GERDA.Configuration.IDomainUiService,
            TicketMasala.Web.Engine.GERDA.Configuration.DomainUiService>();

        if (File.Exists(gerdaConfigPath))
        {
            var gerdaConfigJson = File.ReadAllText(gerdaConfigPath);
            var gerdaConfig = System.Text.Json.JsonSerializer.Deserialize<GerdaConfig>(gerdaConfigJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (gerdaConfig != null)
            {
                builder.Services.AddSingleton(gerdaConfig);
                builder.Services.AddScoped<IGroupingService, GroupingService>();
                builder.Services.AddScoped<IEstimatingService, EstimatingService>();

                // Strategy Factory & Strategies (only if not already registered)
                builder.Services.TryAddScoped<TicketMasala.Web.Engine.GERDA.Strategies.IStrategyFactory, TicketMasala.Web.Engine.GERDA.Strategies.StrategyFactory>();
                builder.Services.AddScoped<IJobRankingStrategy, WeightedShortestJobFirstStrategy>();
                builder.Services.AddScoped<IJobRankingStrategy, SeasonalPriorityStrategy>();
                builder.Services.AddScoped<IEstimatingStrategy, CategoryBasedEstimatingStrategy>();

                // DISPATCHING ARCHITECTURE (Issue #7: Consolidated Implementation)
                // Old: Multiple competing paths (Strategy-based + Generic Engine)
                // New: Single consolidated path using AgentMatchingEngine + IAffinityScorer plugins

                // Core dispatching configuration
                builder.Services.AddScoped<DispatchingConfig>(sp =>
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

                // ML.NET Prediction Engine Pool (for affinity scoring)
                var modelPath = Path.Combine(builder.Environment.ContentRootPath, "gerda_dispatch_model.zip");
                builder.Services.AddPredictionEnginePool<TicketMasala.Web.Engine.GERDA.Models.AgentCustomerRating, TicketMasala.Web.Engine.GERDA.Models.RatingPrediction>()
                    .FromFile(modelName: "GerdaDispatchModel", filePath: modelPath, watchForChanges: true);

                // IAffinityScorer plugin (Matrix Factorization ML-based affinity scoring)
                builder.Services.AddScoped<IAffinityScorer, MatrixFactorizationAffinityScorer>();

                // Core Agent Matching Engine (with IAffinityScorer injected)
                builder.Services.AddScoped<AgentMatchingEngine>(sp =>
                {
                    var config = sp.GetRequiredService<DispatchingConfig>();
                    var logger = sp.GetRequiredService<ILogger<AgentMatchingEngine>>();
                    var affinityScorer = sp.GetService<IAffinityScorer>(); // Optional for backward compatibility
                    return new AgentMatchingEngine(config, logger, affinityScorer);
                });

                // Legacy strategy kept for fallback compatibility (optional)
                builder.Services.AddScoped<IDispatchingStrategy, MatrixFactorizationDispatchingStrategy>();
                builder.Services.AddScoped<IDispatchingStrategy, ZoneBasedDispatchingStrategy>();

                builder.Services.AddScoped<TicketMasala.Web.Engine.GERDA.Features.IFeatureExtractor, TicketMasala.Web.Engine.GERDA.Features.DynamicFeatureExtractor>();

                builder.Services.AddScoped<IRankingService, RankingService>();
                builder.Services.AddScoped<IDispatchingStrategySelector, DomainDispatchingStrategySelector>();
                builder.Services.AddScoped<IAutoDispatchPolicy, ScoreThresholdAutoDispatchPolicy>();
                builder.Services.AddScoped<IProjectManagerRecommendationService, WorkloadAndSuccessProjectManagerRecommendationService>();

                // CONSOLIDATED DISPATCHING SERVICE (Issue #7)
                // Uses AgentMatchingEngine as primary path with IAffinityScorer plugin
                builder.Services.AddScoped<IDispatchingService, DispatchingService>();
                builder.Services.AddScoped<IDispatchBacklogService, DispatchBacklogService>();
                builder.Services.AddScoped<IAnticipationService, AnticipationService>();
                builder.Services.AddScoped<IKnowledgeService, KnowledgeService>();
                builder.Services.AddScoped<IGerdaService, GerdaService>();
                builder.Services.AddHostedService<GerdaBackgroundService>();

                StrategyAutoRegistration.RegisterPluginStrategies(builder.Services);
            }
        }
        else
        {
            builder.Services.AddScoped<IDispatchingService, NoOpDispatchingService>();
            builder.Services.AddScoped<IKnowledgeService, NoOpKnowledgeService>();
            builder.Services.AddScoped<IGerdaService, NoOpGerdaService>();
        }

        // Rate Limiting
        builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status429TooManyRequests;

                options.AddFixedWindowLimiter("api", opt =>
                {
                    opt.PermitLimit = 100;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 10;
                });

                options.AddSlidingWindowLimiter("login", opt =>
                {
                    opt.PermitLimit = 5;
                    opt.Window = TimeSpan.FromMinutes(15);
                    opt.SegmentsPerWindow = 3;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });

                options.AddTokenBucketLimiter("general", opt =>
                {
                    opt.TokenLimit = 50;
                    opt.TokensPerPeriod = 10;
                    opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 5;
                });

                // Strict limiter for external ticket submissions (anti-spam)
                options.AddFixedWindowLimiter("ExternalSubmission", opt =>
                {
                    opt.PermitLimit = 3; // Max 3 submissions per window
                    opt.Window = TimeSpan.FromMinutes(5); // Per 5 minutes
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0; // No queue, immediate 429
                });
            });

        // Memory Cache - registered once here, used throughout the application
        builder.Services.AddMemoryCache();
        builder.Services.AddDistributedMemoryCache();

        // Data Protection
        if (builder.Environment.IsProduction())
        {
            var keyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "keys");
            Directory.CreateDirectory(keyPath);
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keyPath))
                .SetApplicationName("ticket-masala");
        }
        else
        {
            builder.Services.AddDataProtection()
                .SetApplicationName("ticket-masala");
        }

        // WebOptimizer
        builder.Services.AddWebOptimizer(pipeline =>
        {
            pipeline.AddCssBundle("/css/bundle.css",
                "lib/bootstrap/dist/css/bootstrap.min.css",
                "css/design-system.css",
                "css/site.css")
                .MinifyCss();
            pipeline.AddJavaScriptBundle("/js/bundle.js",
                "lib/jquery/dist/jquery.min.js",
                "lib/bootstrap/dist/js/bootstrap.bundle.min.js",
                "js/site.js",
                "js/toast.js")
                .MinifyJavaScript();
        });

        // Authorization
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AllowAnonymous", policy => policy.RequireAssertion(_ => true));
            if (!builder.Environment.IsDevelopment())
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            }
        });

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            // Secure cookie in Production to prevent transmission over HTTP
            options.Cookie.SecurePolicy = builder.Environment.IsProduction()
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.LoginPath = "/Identity/Account/Login";
            options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            options.SlidingExpiration = true;
        });

        // Localization & CORS
        builder.Services.AddLocalization();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });

        builder.Services.AddControllersWithViews()
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization();

        builder.Services.Configure<RazorViewEngineOptions>(options =>
        {
            options.ViewLocationExpanders.Add(new TenantViewLocationExpander());
        });

        builder.Services.AddRazorPages();
        builder.Services.AddHealthChecks();
        // Note: TenantConnectionResolver is already registered by AddMasalaDatabase()

        // Swagger
        builder.Services.AddEndpointsApiExplorer();

        // Forwarded Headers
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return builder;
    }
}
