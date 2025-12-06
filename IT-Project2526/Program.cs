using IT_Project2526;
using IT_Project2526.Data;
using IT_Project2526.Models;
using IT_Project2526.Services;
using IT_Project2526.Repositories;
using IT_Project2526.Observers;
using IT_Project2526.Health;
using IT_Project2526.Middleware;
using IT_Project2526.Services.GERDA;
using IT_Project2526.Services.GERDA.Models;
using IT_Project2526.Services.GERDA.Grouping;
using IT_Project2526.Services.GERDA.Estimating;
using IT_Project2526.Services.GERDA.Ranking;
using IT_Project2526.Services.GERDA.Dispatching;
using IT_Project2526.Services.GERDA.Anticipation;
using IT_Project2526.Services.GERDA.BackgroundJobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Localization;
using WebOptimizer;

var builder = WebApplication.CreateBuilder(args);

// Database Configuration via appsettings.json / appsettings.Production.json
var dbProvider = builder.Configuration["DatabaseProvider"];
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ITProjectDB>(options =>
{
    if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        // Ensure /data directory exists for SQLite in production/docker
        if (connectionString != null && connectionString.Contains("/data/"))
        {
            var dataDir = "/data";
            if (!Directory.Exists(dataDir))
            {
                Console.WriteLine($"Creating database directory: {dataDir}");
                Directory.CreateDirectory(dataDir);
            }
        }
        
        Console.WriteLine($"Using SQLite Provider with connection: {connectionString}");
        options.UseSqlite(connectionString);
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
    .AddEntityFrameworkStores<ITProjectDB>()
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
builder.Services.AddSingleton<IT_Project2526.Services.Configuration.IDomainConfigurationService, 
    IT_Project2526.Services.Configuration.DomainConfigurationService>();

// Custom Field Validation Service
builder.Services.AddScoped<IT_Project2526.Services.Validation.ICustomFieldValidationService,
    IT_Project2526.Services.Validation.CustomFieldValidationService>();

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
builder.Services.AddHostedService<QueuedHostedService>();
builder.Services.AddHostedService<TicketGeneratorService>();
builder.Services.AddScoped<ITicketGenerator, TicketGenerator>();

// Register DbSeeder
builder.Services.AddScoped<DbSeeder>();

// ============================================
// GERDA AI Services Configuration
// ============================================
var gerdaConfigPath = Path.Combine(builder.Environment.ContentRootPath, "masala_config.json");
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
        builder.Services.AddScoped<IRankingService, RankingService>();
        builder.Services.AddScoped<IDispatchingService, DispatchingService>();
        builder.Services.AddScoped<IAnticipationService, AnticipationService>();
        builder.Services.AddScoped<IGerdaService, GerdaService>();
        
        // Register GERDA Background Service for automated maintenance
        builder.Services.AddHostedService<GerdaBackgroundService>();
        
        Console.WriteLine("GERDA AI Services registered successfully (G+E+R+D+A + Background Jobs)");
    }
}
else
{
    Console.WriteLine($"Warning: GERDA config not found at {gerdaConfigPath}");
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
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo("/data/keys"))
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

// Configure Forwarded Headers for Fly.io (Proxy)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    // Trust all proxies (Fly.io internal network)
    options.KnownNetworks.Clear();
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
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

app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

public partial class Program { }
