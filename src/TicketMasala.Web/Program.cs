using TicketMasala.Web;
using TicketMasala.Web.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Tenancy;
using TicketMasala.Web.Extensions;
using TicketMasala.Web.Configuration;

using TicketMasala.Web.Health;
using TicketMasala.Web.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Localization;
using WebOptimizer;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

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
TenantPluginLoader.LoadPlugins(builder, pluginPath ?? "");

// ============================================
// DATABASE CONFIGURATION
// ============================================
builder.Services.AddMasalaDatabase(builder.Configuration, builder.Environment);

// ============================================
// IDENTITY CONFIGURATION
// ============================================
builder.Services.AddMasalaIdentity();
builder.Services.ConfigureMasalaCookie();

// ============================================
// REPOSITORIES & OBSERVERS
// ============================================
builder.Services.AddRepositories();
builder.Services.AddObservers();

// ============================================
// CORE BUSINESS SERVICES
// ============================================
builder.Services.AddCoreServices();
builder.Services.AddBackgroundServices();

// ============================================
// GERDA AI SERVICES
// ============================================
var configBasePath = TicketMasala.Web.Configuration.ConfigurationPaths.GetConfigBasePath(builder.Environment.ContentRootPath);
builder.Services.AddGerdaServices(builder.Environment, configBasePath);
builder.Services.AddScoped<TicketMasala.Domain.Services.IExplainabilityService, TicketMasala.Web.Engine.GERDA.Explainability.ExplainabilityService>();

// ============================================
// INFRASTRUCTURE & SECURITY
// ============================================
builder.Services.AddMasalaMonitoring();
builder.Services.AddMasalaSecurity(builder.Environment);
builder.Services.AddMasalaApi();
builder.Services.AddMasalaFrontend();

// ============================================
// CACHING & UTILITIES
// ============================================
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<TenantConnectionResolver>();

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
