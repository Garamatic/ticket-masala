using System.IO;
using System.Reflection;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Prometheus;
using TicketMasala.Domain.Entities;
using TicketMasala.Web;
using TicketMasala.Web.Configuration;
using TicketMasala.Web.Data;
using TicketMasala.Web.Extensions;
using TicketMasala.Web.Health;
using TicketMasala.Web.Middleware;
using TicketMasala.Web.AI;
using TicketMasala.Web.Services;
using RabbitMqConnector;
using TicketMasala.Web.Tenancy;
using WebOptimizer;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// LOAD .env (local development)
// ============================================
var envPath = Path.Combine(builder.Environment.ContentRootPath, ".env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
}

// ============================================
// STRONGLY-TYPED CONFIGURATION
// ============================================
builder.Services.AddMasalaConfiguration(builder.Configuration);

// Validate configuration on startup
using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger("Startup");
builder.Services.ValidateMasalaConfiguration(builder.Configuration, logger);

// ============================================
// LOCALIZATION CONFIGURATION
// ============================================
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "en", "nl", "fr" };
    options.SetDefaultCulture("en");
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
});

// ============================================
// TENANT PLUGIN SYSTEM
// ============================================
var pluginPath = Environment.GetEnvironmentVariable("MASALA_PLUGINS_PATH");
TenantPluginLoader.LoadPlugins(builder, pluginPath ?? "", logger);

// ============================================
// DATABASE CONFIGURATION
// ============================================
builder.Services.AddMasalaDatabase(builder.Configuration, builder.Environment);

// ============================================
// CORE BUSINESS SERVICES
// ============================================
builder.AddMasalaCore();

// ============================================
// GERDA AI SERVICES (Deep Module)
// ============================================
var configBasePath = TicketMasala.Web.Configuration.ConfigurationPaths.GetConfigBasePath(builder.Environment.ContentRootPath);
builder.Services.AddGerda(options =>
{
    options.ConfigBasePath = configBasePath;
    options.ModelPath = builder.Environment.ContentRootPath;
});
builder.Services.LogGerdaConfiguration(logger);

// ============================================
// OPENTELEMETRY TRACING
// ============================================
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(
        serviceName: "TicketMasala.Web",
        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0"))
    .WithTracing(t =>
    {
        // Explicitly include our application ActivitySources
        t.AddSource("TicketMasala.Outbox");
        t.AddSource("RabbitMqConnector.Messaging");
        // GERDA deep module registers its own sources internally via AddGerda()
        // AspNetCore instrumentation for HTTP request spans
        t.AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.Filter = httpContext =>
            {
                // Skip health checks and metrics endpoints
                var path = httpContext.Request.Path.Value ?? "";
                return !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                    && !path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase);
            };
        });
        // Instrument outgoing HTTP calls (OpenRouter, etc.)
        t.AddHttpClientInstrumentation(options =>
        {
            options.RecordException = true;
        });
        // OTLP exporter for production telemetry
        // t.AddOtlpExporter();
    });

// Configure OpenRouter HTTP client with retry policy
builder.Services.AddHttpClient("OpenRouter", client =>
{
    var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
        ?? builder.Configuration["OpenAI:ApiKey"]
        ?? "";

    var baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL")
        ?? builder.Configuration["OpenAI:BaseUrl"]
        ?? "https://openrouter.ai/api/v1";

    // Normalize base URL
    if (!baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
    {
        baseUrl = baseUrl.TrimEnd('/') + "/v1";
    }

    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
    client.DefaultRequestHeaders.Add("HTTP-Referer", "https://ticketmasala.com");
    client.DefaultRequestHeaders.Add("X-Title", "TicketMasala");
    client.Timeout = TimeSpan.FromSeconds(60);
})
.AddTransientHttpErrorPolicy(policy => policy
    .WaitAndRetryAsync(3, retryAttempt =>
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

// Register OpenAI service for explainability
// AI Generation Port (Domain-facing, provider-agnostic)
builder.Services.AddTransient<TicketMasala.Domain.Ports.IAIGenerationPort>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<OpenAiSettings>>().Value;
    var provider = settings.Provider?.ToLowerInvariant() ?? "openai";

    return provider switch
    {
        "openai" or "openrouter" or "ollama" or "llamacpp" =>
            ActivatorUtilities.CreateInstance<OpenAIGenerationAdapter>(sp),
        _ => throw new NotSupportedException($"LLM provider '{provider}' is not supported.")
    };
});
builder.Services.Configure<TicketMasala.Web.AI.AIOperationRegistry>(
    builder.Configuration.GetSection("AI:Operations"));

// Legacy OpenAI service (deprecated — delegates to the new port)
builder.Services.AddTransient<TicketMasala.Web.AI.IOpenAiService, TicketMasala.Web.AI.OpenAiService>();
builder.Services.AddScoped<TicketMasala.Domain.Services.IExplainabilityService, TicketMasala.Web.Engine.GERDA.Explainability.ExplainabilityService>();

// ============================================
// INFRASTRUCTURE & SECURITY
// ============================================
builder.Services.AddHttpContextAccessor(); // Required for services that need HttpContext
builder.Services.AddMasalaApi();
builder.Services.AddMasalaFrontend();
builder.Services.AddScoped<TicketMasala.Web.Facades.ITicketContextFacade, TicketMasala.Web.Facades.TicketContextFacade>();

var app = builder.Build();

// ============================================
// CONFIGURE MIDDLEWARE & ENDPOINTS
// ============================================

app.UseMasalaCore(app.Environment);

app.UseAuthentication();
app.UseAuthorization();


app.MapMetrics(); // Prometheus metrics
app.MapMasalaEndpoints();

// ============================================
// INITIALIZE SERVICES
// ============================================
await app.InitializeMasalaCoreAsync();

app.Run();

public partial class Program { }
