using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Data;
using TicketMasala.Web.Data.Seeding;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.Enrichment;
using TicketMasala.Web.Engine.GERDA.Sentiment;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Engine.Projects;
using TicketMasala.Web.Engine.Security;
using TicketMasala.Web.Infrastructure.DomainEvents;
using TicketMasala.Web.Modules.Tickets;
using TicketMasala.Web.Observers;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.Services;
using TicketMasala.Web.Tenancy;

namespace TicketMasala.Web.Extensions;

/// <summary>
/// Extension methods to configure Ticket Masala core services.
/// Use these in your application's Program.cs to add all required services.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Adds all Ticket Masala core services to the application.
    /// Composes smaller, focused extension methods for better maintainability.
    /// </summary>
    public static WebApplicationBuilder AddMasalaCore(this WebApplicationBuilder builder)
    {
        builder
            .AddMasalaIdentity()
            .AddMasalaModules()
            .AddMasalaCrossCutting()
            .AddMasalaInfrastructure()
            .AddMasalaMessaging()
            .AddMasalaSecurity();

        return builder;
    }

    /// <summary>
    /// Adds Identity, Authentication, and Authorization services.
    /// </summary>
    private static WebApplicationBuilder AddMasalaIdentity(this WebApplicationBuilder builder)
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
            options.Cookie.SecurePolicy = builder.Environment.IsProduction()
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.LoginPath = "/Identity/Account/Login";
            options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            options.SlidingExpiration = true;
        });

        return builder;
    }

    /// <summary>
    /// Adds all feature modules (Tickets, Projects, Knowledge, Ingestion, Enrichment).
    /// </summary>
    private static WebApplicationBuilder AddMasalaModules(this WebApplicationBuilder builder)
    {
        // Core domain modules
        builder.Services.AddTicketModule();
        builder.Services.AddProjectModule();
        builder.Services.AddKnowledgeModule();
        builder.Services.AddIngestionModule();
        builder.Services.AddEnrichmentModule();

        // Remaining repositories
        builder.Services.AddScoped<IUserRepository, EfCoreUserRepository>();

        // Observers
        builder.Services.AddScoped<ICommentObserver, LoggingCommentObserver>();
        builder.Services.AddScoped<ICommentObserver, NotificationCommentObserver>();

        return builder;
    }

    /// <summary>
    /// Adds cross-cutting services (Clock, JSON, File, Email, Notifications, etc).
    /// </summary>
    private static WebApplicationBuilder AddMasalaCrossCutting(this WebApplicationBuilder builder)
    {
        // System abstractions
        builder.Services.AddSingleton<TicketMasala.Web.Abstractions.ISystemClock, TicketMasala.Web.Services.SystemClock>();
        builder.Services.AddScoped<TicketMasala.Web.Services.IJsonParsingService, TicketMasala.Web.Services.JsonParsingService>();

        // Core services
        builder.Services.AddScoped<IFileStorageService, DiskFileStorageService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddScoped<ISavedFilterService, SavedFilterService>();
        builder.Services.AddScoped<IAuditService, AuditService>();
        builder.Services.AddScoped<IMetricsService, MetricsService>();

        // Security module
        builder.Services.AddSecurityModule();

        // GERDA Configuration
        builder.Services.AddSingleton<TicketMasala.Web.Engine.GERDA.Configuration.IDomainConfigurationService,
            TicketMasala.Web.Engine.GERDA.Configuration.DomainConfigurationService>();
        builder.Services.AddScoped<TicketMasala.Web.Engine.GERDA.Configuration.IDomainUiService,
            TicketMasala.Web.Engine.GERDA.Configuration.DomainUiService>();

        // Sentiment Analysis
        builder.Services.AddScoped<ISentimentAnalyzer, SimpleSentimentAnalyzer>();

        // Data Seeding
        builder.Services.AddDataSeeding();

        return builder;
    }

    /// <summary>
    /// Adds infrastructure services (Domain Events, CORS, Health Checks, Swagger, Forwarded Headers).
    /// </summary>
    private static WebApplicationBuilder AddMasalaInfrastructure(this WebApplicationBuilder builder)
    {
        // Domain Events Infrastructure
        builder.Services.AddDomainEvents();

        // CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });

        // Health Checks
        builder.Services.AddHealthChecks();

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

        // View Location Expander for multi-tenancy
        builder.Services.Configure<RazorViewEngineOptions>(options =>
        {
            options.ViewLocationExpanders.Add(new TenantViewLocationExpander());
        });

        return builder;
    }

    /// <summary>
    /// Adds messaging infrastructure (RabbitMQ, Outbox, Background Services).
    /// </summary>
    private static WebApplicationBuilder AddMasalaMessaging(this WebApplicationBuilder builder)
    {
        // RabbitMQ Publisher (outbound events) - lazy connection
        builder.Services.AddSingleton<TicketMasala.Web.Messaging.IRabbitMqPublisher, TicketMasala.Web.Messaging.RabbitMqPublisher>();

        // Outbox Publisher (Background Service)
        builder.Services.AddSingleton<OutboxPublisherOptions>(sp =>
            builder.Configuration.GetSection("OutboxPublisher").Get<OutboxPublisherOptions>() ?? new OutboxPublisherOptions());
        builder.Services.AddHostedService<OutboxPublisher>();

        return builder;
    }

    /// <summary>
    /// Adds security services (Rate Limiting, Data Protection, Caching).
    /// </summary>
    private static WebApplicationBuilder AddMasalaSecurity(this WebApplicationBuilder builder)
    {
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
                opt.PermitLimit = 3;
                opt.Window = TimeSpan.FromMinutes(5);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });
        });

        // Memory Cache
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

        return builder;
    }
}
