using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace TicketMasala.Web.Extensions;

public static class SecurityServiceCollectionExtensions
{
    public static IServiceCollection AddMasalaSecurity(this IServiceCollection services, IWebHostEnvironment env)
    {
        // Rate Limiting
        services.AddRateLimiter(options =>
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

        // Data Protection
        if (env.IsProduction())
        {
            var keyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "keys");
            Directory.CreateDirectory(keyPath);

            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keyPath))
                .SetApplicationName("ticket-masala");
        }
        else
        {
            services.AddDataProtection()
                .SetApplicationName("ticket-masala");
        }

        // Authorization
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AllowAnonymous", policy => policy.RequireAssertion(_ => true));

            if (!env.IsDevelopment())
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            }
        });

        // CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                builder => builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
        });

        // Forwarded Headers
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }
}
