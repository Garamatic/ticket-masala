using TicketMasala.Web;
using TicketMasala.Web.Data;
using TicketMasala.Web.Models;

using TicketMasala.Web.Repositories;
using TicketMasala.Web.Observers;
using TicketMasala.Web.Health;
using TicketMasala.Web.Middleware;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets; // Updated
using TicketMasala.Web.Engine.Projects;     // Updated
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
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Localization;
using WebOptimizer;
using Microsoft.Extensions.ML; // PredictionEnginePool

var builder = WebApplication.CreateBuilder(args);

// Database Configuration via appsettings.json / appsettings.Production.json
var dbProvider = builder.Configuration["DatabaseProvider"];
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Database Configuration via appsettings.json / appsettings.Production.json
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<MasalaDbContext>(options =>
    {
        if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            // Ensure data directory exists for SQLite in containerized environments
            if (connectionString != null)
            {
                // Extract the Data Source path from connection string
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
            // Suppress pending model changes warning to allow EnsureCreated to work
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        }
        else
        {
            // Default to SQL Server
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

//Identity configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredUniqueChars = 2;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;

    // SignIn settings
    options.SignIn.RequireConfirmedEmail = false;
})
    .AddEntityFrameworkStores<MasalaDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI(); //for identity pages

// Register Managers (removed ApplicationUserManager - merged into IUserRepository)

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

// Project Observers
builder.Services.AddScoped<IProjectObserver, LoggingProjectObserver>();
builder.Services.AddScoped<IProjectObserver, NotificationProjectObserver>();

// Comment Observers
builder.Services.AddScoped<ICommentObserver, LoggingCommentObserver>();
builder.Services.AddScoped<ICommentObserver, NotificationCommentObserver>();


// ============================================
// Register Services (CQRS + Factory Pattern)
// ============================================


// Domain Configuration Service (loads masala_domains.yaml)
// NOTE: DomainConfigurationService depends on GERDA rule/compiler services.
// Register the RuleCompilerService globally; register the DomainConfigurationService
// only if GERDA configuration is present to avoid DI activation failures in
// lightweight container images that don't include GERDA config files.
builder.Services.AddSingleton<TicketMasala.Web.Engine.Compiler.RuleCompilerService>();

// Custom Field Validation Service
builder.Services.AddScoped<TicketMasala.Web.Engine.Ingestion.Validation.ICustomFieldValidationService,
    TicketMasala.Web.Engine.Ingestion.Validation.CustomFieldValidationService>();

// Rule Engine Service
builder.Services.AddScoped<TicketMasala.Web.Engine.Compiler.IRuleEngineService,
    TicketMasala.Web.Engine.Compiler.RuleEngineService>();

builder.Services.AddScoped<IMetricsService, MetricsService>();

// TicketService implements all three interfaces (for backward compatibility)
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<ITicketService>(sp => sp.GetRequiredService<TicketService>());
builder.Services.AddScoped<ITicketQueryService>(sp => sp.GetRequiredService<TicketService>());
builder.Services.AddScoped<ITicketCommandService>(sp => sp.GetRequiredService<TicketService>());

// Factory Pattern for Ticket creation
builder.Services.AddScoped<ITicketFactory, TicketFactory>();

builder.Services.AddScoped<IDispatchBacklogService, DispatchBacklogService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ITicketImportService, TicketImportService>();
builder.Services.AddHostedService<EmailIngestionService>();

// Register Background Queue
builder.Services.AddSingleton<IBackgroundTaskQueue>(ctx => 
{
    return new BackgroundQueue(100); // Capacity of 100 items
});
builder.Services.AddSingleton(System.Threading.Channels.Channel.CreateUnbounded<TicketMasala.Web.ViewModels.Ingestion.IngestionWorkItem>());
builder.Services.AddHostedService<QueuedHostedService>();
builder.Services.AddHostedService<TicketGeneratorService>();
builder.Services.AddScoped<ITicketGenerator, TicketGenerator>();

// Register DbSeeder
builder.Services.AddScoped<DbSeeder>();

// ============================================
// GERDA AI Services Configuration
// ============================================
var configBasePath = TicketMasala.Web.Configuration.ConfigurationPaths.GetConfigBasePath(builder.Environment.ContentRootPath);
var gerdaConfigPath = Path.Combine(configBasePath, "masala_config.json");

Console.WriteLine($"Loading configuration from: {configBasePath}");

if (File.Exists(gerdaConfigPath))
{
    var gerdaConfigJson = File.ReadAllText(gerdaConfigPath);
    var gerdaConfig = System.Text.Json.JsonSerializer.Deserialize<GerdaConfig>(gerdaConfigJson, 
        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    
    if (gerdaConfig != null)
    {
        builder.Services.AddSingleton(gerdaConfig);
        // Domain Configuration Service (loads masala_domains.yaml)
        builder.Services.AddSingleton<TicketMasala.Web.Engine.GERDA.Configuration.IDomainConfigurationService,
            TicketMasala.Web.Engine.GERDA.Configuration.DomainConfigurationService>();
        builder.Services.AddScoped<IGroupingService, GroupingService>();
        builder.Services.AddScoped<IEstimatingService, EstimatingService>();
        
        // Strategy Factory & Strategies
        builder.Services.AddScoped<TicketMasala.Web.Engine.GERDA.Strategies.IStrategyFactory, TicketMasala.Web.Engine.GERDA.Strategies.StrategyFactory>();
        builder.Services.AddScoped<IJobRankingStrategy, WeightedShortestJobFirstStrategy>();
        // Register the seasonal priority strategy so domains that reference it can be validated
        builder.Services.AddScoped<IJobRankingStrategy, SeasonalPriorityStrategy>();
        builder.Services.AddScoped<IEstimatingStrategy, CategoryBasedEstimatingStrategy>();
        builder.Services.AddScoped<IDispatchingStrategy, MatrixFactorizationDispatchingStrategy>();
        builder.Services.AddScoped<IDispatchingStrategy, ZoneBasedDispatchingStrategy>();
        // RuleCompilerService registered globally above
        
        // AI Features
        builder.Services.AddScoped<TicketMasala.Web.Engine.GERDA.Features.IFeatureExtractor, TicketMasala.Web.Engine.GERDA.Features.DynamicFeatureExtractor>();

        // Register PredictionEnginePool for Scalability (09-scalability.md)
        // This is strictly In-Process (as per critique), avoiding model reload overhead per request.
        var modelPath = Path.Combine(builder.Environment.ContentRootPath, "gerda_dispatch_model.zip");
        builder.Services.AddPredictionEnginePool<TicketMasala.Web.Engine.GERDA.Models.AgentCustomerRating, TicketMasala.Web.Engine.GERDA.Models.RatingPrediction>()
            .FromFile(modelName: "GerdaDispatchModel", filePath: modelPath, watchForChanges: true);

        builder.Services.AddScoped<IRankingService, RankingService>();
        builder.Services.AddScoped<IDispatchingService, DispatchingService>();
        builder.Services.AddScoped<IAnticipationService, AnticipationService>();
        builder.Services.AddScoped<IGerdaService, GerdaService>();
        
        // Register GERDA Background Service for automated maintenance
        builder.Services.AddHostedService<GerdaBackgroundService>();
        
        Console.WriteLine("GERDA AI services initialized successfully");
    }
}
else
{
    Console.WriteLine($"Note: GERDA config not found at {gerdaConfigPath}");
    Console.WriteLine("Application will run with basic ticketing functionality");
}


// Add Memory Cache
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

// ============================================
// Rate Limiting Configuration
// ============================================
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Fixed window rate limiter for API endpoints
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });
    
    // Sliding window for login attempts (stricter)
    options.AddSlidingWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(15);
        opt.SegmentsPerWindow = 3;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
    
    // Token bucket for general requests
    options.AddTokenBucketLimiter("general", opt =>
    {
        opt.TokenLimit = 50;
        opt.TokensPerPeriod = 10;
        opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
    });
});

// ============================================
// Enhanced Health Checks
// ============================================
builder.Services.AddHealthChecks()
    .AddCheck<GerdaHealthCheck>("gerda-ai")
    .AddCheck<EmailIngestionHealthCheck>("email-ingestion")
    .AddCheck<BackgroundQueueHealthCheck>("background-queue");

// Persist DataProtection keys so cookies remain valid across restarts
if (builder.Environment.IsProduction())
{
    // Use a writable path for keys in production/pilot (e.g., ./keys/ relative to app)
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

// Configure WebOptimizer for CSS/JS bundling and minification
builder.Services.AddWebOptimizer(pipeline =>
{
    // Bundle and minify CSS files
    pipeline.AddCssBundle("/css/bundle.css",
        "lib/bootstrap/dist/css/bootstrap.min.css",
        "css/design-system.css",
        "css/site.css")
        .MinifyCss();
    
    // Bundle and minify JavaScript files
    pipeline.AddJavaScriptBundle("/js/bundle.js",
        "lib/jquery/dist/jquery.min.js",
        "lib/bootstrap/dist/js/bootstrap.bundle.min.js",
        "js/site.js",
        "js/toast.js")
        .MinifyJavaScript();
});

//Authorization
builder.Services.AddAuthorization(options =>
{
    if (!builder.Environment.IsDevelopment())
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    }
});

builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie settings
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(120);

    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
});

// Add services to the container.
builder.Services.AddLocalization();

// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", 
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.AddControllersWithViews()
    .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

builder.Services.AddRazorPages(); // keep Razor Pages for Identity UI
builder.Services.AddHealthChecks();

// ============================================
// OpenAPI/Swagger Documentation (v3.0 MVP)
// ============================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Ticket Masala API",
        Version = "v1",
        Description = "Configuration-driven work management API. Valid DomainId values are sourced from masala_domains.yaml configuration."
    });
    
    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configure Forwarded Headers for Fly.io (Proxy)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    // Trust all proxies (Fly.io internal network)
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Forward headers must be first middleware
app.UseForwardedHeaders();

// Localization Configuration
var supportedCultures = new[] { "en", "fr", "nl" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

// Add cookie provider for language switcher support
localizationOptions.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());

app.UseRequestLocalization(localizationOptions);

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("==================================================");
        logger.LogInformation("Checking if database seeding is needed...");
        logger.LogInformation("==================================================");
        
        var seeder = services.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync();
        
        logger.LogInformation("==================================================");
        logger.LogInformation("Database check completed");
        logger.LogInformation("==================================================");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "==================================================");
        logger.LogError(ex, "CRITICAL: An error occurred while seeding the database.");
        logger.LogError(ex, "==================================================");
        logger.LogError("You may need to manually create test users or check your database connection");
    }
}

// Validate AI Strategies Configuration
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        // Attempt to retrieve the DomainConfigurationService. If dependencies are missing
        // (e.g. partial GERDA registration in lightweight container), this can throw.
        var domainService = services.GetService<TicketMasala.Web.Engine.GERDA.Configuration.IDomainConfigurationService>();
        if (domainService == null)
        {
            logger.LogInformation("GERDA disabled or not configured; skipping AI strategies validation.");
            goto _skip_gerda_validation;
        }

        var strategyFactory = services.GetService<TicketMasala.Web.Engine.GERDA.Strategies.IStrategyFactory>();
        if (strategyFactory == null)
        {
            logger.LogInformation("GERDA strategy factory not registered; skipping AI strategies validation.");
            goto _skip_gerda_validation;
        }

        logger.LogInformation("==================================================");
        logger.LogInformation("Validating AI Strategy Implementations...");
        logger.LogInformation("==================================================");

        var domains = domainService.GetAllDomains();
        foreach (var domain in domains.Values)
        {
            try
            {
                var rankingName = domain.AiStrategies?.Ranking?.StrategyName ?? "WSJF";
                strategyFactory.GetStrategy<TicketMasala.Web.Engine.GERDA.Ranking.IJobRankingStrategy, double>(rankingName);

                var estimatingName = domain.AiStrategies?.Estimating ?? "CategoryLookup";
                strategyFactory.GetStrategy<TicketMasala.Web.Engine.GERDA.Estimating.IEstimatingStrategy, int>(estimatingName);

                var dispatchingName = domain.AiStrategies?.Dispatching ?? "MatrixFactorization";
                strategyFactory.GetStrategy<TicketMasala.Web.Engine.GERDA.Dispatching.IDispatchingStrategy, List<(string AgentId, double Score)>>(dispatchingName);
                
                logger.LogInformation("Domain '{Domain}' configured strategies validated successfully.", domain.DisplayName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CRITICAL: Configuration error for domain '{Domain}'. Validation FAILED.", domain.DisplayName);
                // Continue to next domain without failing the whole app.
                continue;
            }
        }
    }
    catch (Exception ex)
    {
        // Defensive: if something goes wrong while attempting to instantiate GERDA services
        // (missing dependencies or configuration), log and continue instead of crashing the host.
        logger.LogWarning(ex, "GERDA initialization failed during validation; skipping AI strategies validation.");
    }

_skip_gerda_validation:;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    
    // Swagger UI (v3.0 MVP)
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ticket Masala API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Security headers (CSP, X-Frame-Options, etc.)
app.UseSecurityHeaders();

// Rate limiting
app.UseRateLimiter();

// Use WebOptimizer middleware
app.UseWebOptimizer();

// Only redirect to HTTPS in Development (Fly.io proxy handles TLS termination in production)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

// Use CORS
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); 

// Health Check with JSON response (v3.1)
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description
            })
        };
        await context.Response.WriteAsJsonAsync(result);
    }
}).AllowAnonymous();

// Simple metrics endpoint (v3.1) - KISS: uses built-in counters
app.MapGet("/metrics", async (IServiceProvider sp) =>
{
    var metrics = new
    {
        timestamp = DateTime.UtcNow,
        uptime = (DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds,
        memory_mb = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024,
        gc_gen0 = GC.CollectionCount(0),
        gc_gen1 = GC.CollectionCount(1),
        gc_gen2 = GC.CollectionCount(2)
    };
    return Results.Json(metrics);
}).AllowAnonymous();

app.Run();

public partial class Program { }
