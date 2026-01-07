using TicketMasala.Web.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Tenancy;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Observers;
using TicketMasala.Web.Health;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.GERDA.Tickets.Domain;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Engine.Ingestion.Background;
using TicketMasala.Web.Engine.Compiler;
using TicketMasala.Web.Engine.GERDA;
using TicketMasala.Web.Engine.GERDA.Models;
using TicketMasala.Web.Engine.GERDA.Grouping;
using TicketMasala.Web.Engine.GERDA.Estimating;
using TicketMasala.Web.Engine.GERDA.Ranking;
using TicketMasala.Web.Engine.GERDA.Dispatching;
using TicketMasala.Web.Engine.GERDA.Anticipation;
using TicketMasala.Web.Engine.GERDA.BackgroundJobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Threading.RateLimiting;
using Microsoft.Extensions.ML;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        // ============================================
        // TENANT PLUGIN SYSTEM
        // ============================================
        var pluginPath = Environment.GetEnvironmentVariable("MASALA_PLUGINS_PATH");
        TenantPluginLoader.LoadPlugins(builder, pluginPath ?? "");

        // Database Configuration
        var dbProvider = builder.Configuration["DatabaseProvider"];
        var tenantConnectionResolver = new TenantConnectionResolver(builder.Configuration);
        var connectionString = tenantConnectionResolver.GetCurrentConnectionString();

        if (!builder.Environment.IsEnvironment("Testing"))
        {
            builder.Services.AddDbContext<MasalaDbContext>(options =>
            {
                if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
                {
                    if (connectionString != null)
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(
                            connectionString, @"Data Source=([^;]+)");
                        if (match.Success)
                        {
                            var dbPath = match.Groups[1].Value;
                            var dataDir = Path.GetDirectoryName(dbPath);
                            if (!string.IsNullOrEmpty(dataDir) && !Directory.Exists(dataDir))
                            {
                                Console.WriteLine($"Creating database directory: {dataDir}");
                                Directory.CreateDirectory(dataDir);
                            }
                        }
                    }

                    Console.WriteLine($"Using SQLite Provider with connection: {connectionString}");
                    options.UseSqlite(connectionString);
                    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
                }
                else
                {
                    Console.WriteLine($"Using SQL Server Provider");
                    options.UseSqlServer(connectionString, sqlServerOptions =>
                    {
                        sqlServerOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null);
                    });
                }
            });
        }

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
        builder.Services.AddSingleton<RuleCompilerService>();
        builder.Services.AddScoped<TicketMasala.Web.Engine.Ingestion.Validation.ICustomFieldValidationService,
            TicketMasala.Web.Engine.Ingestion.Validation.CustomFieldValidationService>();
        builder.Services.AddScoped<IRuleEngineService, RuleEngineService>();
        builder.Services.AddScoped<TicketMasala.Web.Engine.Security.IPiiScrubberService, 
            TicketMasala.Web.Engine.Security.PiiScrubberService>();
        builder.Services.AddScoped<IMetricsService, MetricsService>();

        // TicketService implements all three interfaces
        builder.Services.AddScoped<TicketService>();
        builder.Services.AddScoped<ITicketService>(sp => sp.GetRequiredService<TicketService>());
        builder.Services.AddScoped<ITicketQueryService>(sp => sp.GetRequiredService<TicketService>());
        builder.Services.AddScoped<ITicketCommandService>(sp => sp.GetRequiredService<TicketService>());

        builder.Services.AddScoped<TicketDispatchService>();
        builder.Services.AddScoped<TicketReportingService>();
        builder.Services.AddScoped<TicketNotificationService>();
        builder.Services.AddScoped<ITicketFactory, TicketFactory>();
        builder.Services.AddScoped<IFileService, FileService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddScoped<ISavedFilterService, SavedFilterService>();
        builder.Services.AddScoped<IProjectService, ProjectService>();
        builder.Services.AddScoped<IAuditService, AuditService>();
        builder.Services.AddScoped<ITicketImportService, TicketImportService>();
        builder.Services.AddHostedService<EmailIngestionService>();

        // Background Queue
        builder.Services.AddSingleton<IBackgroundTaskQueue>(ctx => new BackgroundQueue(100));
        builder.Services.AddSingleton(System.Threading.Channels.Channel.CreateUnbounded<TicketMasala.Web.ViewModels.Ingestion.IngestionWorkItem>());
        builder.Services.AddHostedService<QueuedHostedService>();
        builder.Services.AddHostedService<TicketGeneratorService>();
        builder.Services.AddScoped<ITicketGenerator, TicketGenerator>();

        // DbSeeder
        builder.Services.AddScoped<DbSeeder>();

        // ============================================
        // GERDA AI Services Configuration
        // ============================================
        var configBasePath = TicketMasala.Web.Configuration.ConfigurationPaths.GetConfigBasePath(builder.Environment.ContentRootPath);
        var gerdaConfigPath = Path.Combine(configBasePath, "masala_config.json");

        Console.WriteLine($"Loading configuration from: {configBasePath}");

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
                builder.Services.AddScoped<IDispatchingStrategy, MatrixFactorizationDispatchingStrategy>();
                builder.Services.AddScoped<IDispatchingStrategy, ZoneBasedDispatchingStrategy>();

                builder.Services.AddScoped<TicketMasala.Web.Engine.GERDA.Features.IFeatureExtractor, TicketMasala.Web.Engine.GERDA.Features.DynamicFeatureExtractor>();

                var modelPath = Path.Combine(builder.Environment.ContentRootPath, "gerda_dispatch_model.zip");
                builder.Services.AddPredictionEnginePool<TicketMasala.Web.Engine.GERDA.Models.AgentCustomerRating, TicketMasala.Web.Engine.GERDA.Models.RatingPrediction>()
                    .FromFile(modelName: "GerdaDispatchModel", filePath: modelPath, watchForChanges: true);

                builder.Services.AddScoped<IRankingService, RankingService>();
                builder.Services.AddScoped<IDispatchingService, DispatchingService>();
                builder.Services.AddScoped<IDispatchBacklogService, DispatchBacklogService>();
                builder.Services.AddScoped<IAnticipationService, AnticipationService>();
                builder.Services.AddScoped<IGerdaService, GerdaService>();
                builder.Services.AddHostedService<GerdaBackgroundService>();

                StrategyAutoRegistration.RegisterPluginStrategies(builder.Services);

                Console.WriteLine("GERDA AI Services registered successfully (G+E+R+D+A + Background Jobs)");
            }
        }
        else
        {
            Console.WriteLine($"Note: GERDA config not found at {gerdaConfigPath}");
            Console.WriteLine("Application will run with basic ticketing functionality");
            builder.Services.AddScoped<IDispatchingService, NoOpDispatchingService>();
            builder.Services.AddScoped<IGerdaService, NoOpGerdaService>();
        }

        // Memory Cache
        builder.Services.AddMemoryCache();
        builder.Services.AddDistributedMemoryCache();

        // Rate Limiting
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
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
        });

        // Health Checks
        builder.Services.AddHealthChecks()
            .AddCheck<GerdaHealthCheck>("gerda-ai")
            .AddCheck<EmailIngestionHealthCheck>("email-ingestion")
            .AddCheck<BackgroundQueueHealthCheck>("background-queue");

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
        builder.Services.AddSingleton<TenantConnectionResolver>();

        // Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "Ticket Masala API",
                Version = "v1",
                Description = "Configuration-driven work management API."
            });
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

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
