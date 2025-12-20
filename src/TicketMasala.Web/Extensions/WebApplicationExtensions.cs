using TicketMasala.Web.Data;
using TicketMasala.Web.Tenancy;
using TicketMasala.Web.Middleware;
using Microsoft.AspNetCore.Localization;

namespace TicketMasala.Web.Extensions;

/// <summary>
/// Extension methods to configure Ticket Masala middleware and endpoints.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures the Ticket Masala middleware pipeline.
    /// Call this after building the application.
    /// </summary>
    public static WebApplication UseMasalaCore(this WebApplication app, IWebHostEnvironment env)
    {
        // Forward headers (for reverse proxies)
        app.UseForwardedHeaders();

        // Localization
        var supportedCultures = new[] { "en", "fr", "nl" };
        var localizationOptions = new RequestLocalizationOptions()
            .SetDefaultCulture(supportedCultures[0])
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);
        localizationOptions.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
        app.UseRequestLocalization(localizationOptions);

        // Environment-specific
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
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

        // Security & Rate Limiting
        app.UseSecurityHeaders();
        app.UseRateLimiter();

        // Request logging
        app.UseMiddleware<RequestLoggingMiddleware>();

        // WebOptimizer for bundling
        app.UseWebOptimizer();

        // HTTPS redirect only in dev (proxy handles TLS in prod)
        if (env.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors("AllowAll");

        // Configure tenant plugin middleware
        TenantPluginLoader.ConfigurePluginMiddleware(app, env);

        return app;
    }

    /// <summary>
    /// Maps Ticket Masala endpoints (controllers, razor pages, health checks).
    /// Call this after UseAuthentication/UseAuthorization.
    /// </summary>
    public static WebApplication MapMasalaEndpoints(this WebApplication app)
    {
        // Area routes (for tenant-specific features like EHT)
        app.MapControllerRoute(
            name: "areas",
            pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

        // MVC controller routes (Dispatch UI)
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        // Razor Pages (Identity UI)
        app.MapRazorPages();

        // Health Check with JSON response
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

        // Metrics endpoint
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

        return app;
    }

    /// <summary>
    /// Initializes Ticket Masala services (database seeding, strategy validation).
    /// Call this after mapping endpoints, before app.Run().
    /// </summary>
    public static async Task InitializeMasalaCoreAsync(this WebApplication app)
    {
        // Database seeding
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Checking if Masala database seeding is needed...");
                var seeder = services.GetRequiredService<DbSeeder>();
                await seeder.SeedAsync();
                logger.LogInformation("Masala database check completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding Masala database");
            }
        }

        // Strategy validation
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                var domainService = services.GetService<TicketMasala.Web.Engine.GERDA.Configuration.IDomainConfigurationService>();
                if (domainService == null)
                {
                    logger.LogInformation("GERDA disabled; skipping strategy validation.");
                    return;
                }

                var strategyFactory = services.GetService<TicketMasala.Web.Engine.GERDA.Strategies.IStrategyFactory>();
                if (strategyFactory == null)
                {
                    logger.LogInformation("Strategy factory not registered; skipping validation.");
                    return;
                }

                logger.LogInformation("Validating AI Strategy Implementations...");

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

                        logger.LogInformation("Domain '{Domain}' strategies validated.", domain.DisplayName);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Configuration error for domain '{Domain}'.", domain.DisplayName);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "GERDA initialization failed; skipping validation.");
            }
        }
    }
}
