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
using TicketMasala.Web.Engine.GERDA.Configuration;
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
        // Register Ticket Module (Extracted to deep module)
        // ============================================
        builder.Services.AddTicketModule();

        // ============================================
        // Register Project Module (Extracted to deep module)
        // ============================================
        builder.Services.AddProjectModule();

        // ============================================
        // Register Knowledge Module (Extracted to deep module)
        // ============================================
        builder.Services.AddKnowledgeModule();

        // ============================================
        // Register Remaining Repositories
        // ============================================
        builder.Services.AddScoped<IUserRepository, EfCoreUserRepository>();

        // ============================================
        // Register Comment Observers (Observer Pattern)
        // ============================================
        builder.Services.AddScoped<ICommentObserver, LoggingCommentObserver>();
        builder.Services.AddScoped<ICommentObserver, NotificationCommentObserver>();

        // ============================================
        // Register System Abstractions & Cross-Cutting Services
        // ============================================
        builder.Services.AddSingleton<TicketMasala.Web.Abstractions.ISystemClock, TicketMasala.Web.Services.SystemClock>();
        builder.Services.AddScoped<TicketMasala.Web.Services.IJsonParsingService, TicketMasala.Web.Services.JsonParsingService>();
        builder.Services.AddSecurityModule();
        builder.Services.AddScoped<IMetricsService, MetricsService>();

        // ============================================
        // Register Core Services (CQRS + Factory Pattern)
        // ============================================
        builder.Services.AddScoped<IFileStorageService, DiskFileStorageService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddScoped<ISavedFilterService, SavedFilterService>();
        builder.Services.AddScoped<IAuditService, AuditService>();

        // ============================================
        // Register Ingestion Module (Extracted to deep module)
        // ============================================
        builder.Services.AddIngestionModule();

        // Sentiment Analysis (GERDA subsystem)
        builder.Services.AddScoped<ISentimentAnalyzer, SimpleSentimentAnalyzer>();

        // ============================================
        // Register Enrichment Module (Extracted to deep module)
        // ============================================
        builder.Services.AddEnrichmentModule();

        // ============================================
        // Register Data Seeding (Strategy Pattern - Extracted)
        // ============================================
        builder.Services.AddDataSeeding();

        // ============================================
        // Register GERDA Configuration Services
        // ============================================
        builder.Services.AddSingleton<TicketMasala.Web.Engine.GERDA.Configuration.IDomainConfigurationService,
            TicketMasala.Web.Engine.GERDA.Configuration.DomainConfigurationService>();

        // ============================================
        // Register Domain Events Infrastructure
        // ============================================
        builder.Services.AddDomainEvents();
        builder.Services.AddScoped<TicketMasala.Web.Engine.GERDA.Configuration.IDomainUiService,
            TicketMasala.Web.Engine.GERDA.Configuration.DomainUiService>();

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

        // ============================================
        // RabbitMQ Publisher (outbound events) - lazy connection
        // ============================================
        // Publisher manages its own connection and creates it on first use.
        // This avoids blocking startup if RabbitMQ is temporarily unavailable.
        builder.Services.AddSingleton<TicketMasala.Web.Messaging.IRabbitMqPublisher, TicketMasala.Web.Messaging.RabbitMqPublisher>();

        // Outbox Publisher (Background Service)
        builder.Services.AddSingleton<OutboxPublisherOptions>(sp =>
            builder.Configuration.GetSection("OutboxPublisher").Get<OutboxPublisherOptions>() ?? new OutboxPublisherOptions());
        builder.Services.AddHostedService<OutboxPublisher>();

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

        // WebOptimizer is configured in AddMasalaFrontend()

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

        // Localization is configured in AddMasalaFrontend()
        // CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });

        // MVC and Razor Pages are configured in AddMasalaApi() and AddMasalaFrontend()
        // ViewLocationExpander for multi-tenancy
        builder.Services.Configure<RazorViewEngineOptions>(options =>
        {
            options.ViewLocationExpanders.Add(new TenantViewLocationExpander());
        });
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
