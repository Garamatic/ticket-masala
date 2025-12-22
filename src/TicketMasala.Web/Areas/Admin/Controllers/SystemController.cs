using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.GERDA.Configuration;

namespace TicketMasala.Web.Areas.Admin.Controllers;

/// <summary>
/// System Controller - Health checks, diagnostics, and configuration management.
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class SystemController : Controller
{
    private readonly MasalaDbContext _context;
    private readonly ILogger<SystemController> _logger;
    private readonly HealthCheckService? _healthCheckService;
    private readonly IDomainConfigurationService _domainConfigurationService;

    public SystemController(
        MasalaDbContext context,
        ILogger<SystemController> logger,
        IDomainConfigurationService domainConfigurationService,
        HealthCheckService? healthCheckService = null)
    {
        _context = context;
        _logger = logger;
        _domainConfigurationService = domainConfigurationService;
        _healthCheckService = healthCheckService;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new SystemHealthViewModel
        {
            DatabaseStatus = await CheckDatabaseAsync(),
            ServerTime = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            DotNetVersion = Environment.Version.ToString(),
            HealthChecks = await GetHealthChecksAsync()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ReloadConfiguration()
    {
        try
        {
            _domainConfigurationService.ReloadConfiguration();
            TempData["SuccessMessage"] = "Configuration reloaded successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload configuration");
            TempData["ErrorMessage"] = "Failed to reload configuration: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<string> CheckDatabaseAsync()
    {
        try
        {
            await _context.Database.CanConnectAsync();
            return "Healthy";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return "Unhealthy";
        }
    }

    private async Task<List<HealthCheckItem>> GetHealthChecksAsync()
    {
        var results = new List<HealthCheckItem>();

        if (_healthCheckService != null)
        {
            try
            {
                var report = await _healthCheckService.CheckHealthAsync();
                foreach (var entry in report.Entries)
                {
                    results.Add(new HealthCheckItem
                    {
                        Name = entry.Key,
                        Status = entry.Value.Status.ToString(),
                        Description = entry.Value.Description ?? "",
                        Duration = entry.Value.Duration
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run health checks");
            }
        }

        // Add basic system info
        results.Add(new HealthCheckItem
        {
            Name = "Memory",
            Status = "Healthy",
            Description = $"Working Set: {Environment.WorkingSet / 1024 / 1024} MB",
            Duration = TimeSpan.Zero
        });

        results.Add(new HealthCheckItem
        {
            Name = "Processors",
            Status = "Healthy",
            Description = $"Processor Count: {Environment.ProcessorCount}",
            Duration = TimeSpan.Zero
        });

        return results;
    }
}

public class SystemHealthViewModel
{
    public string DatabaseStatus { get; set; } = "";
    public DateTime ServerTime { get; set; }
    public string Environment { get; set; } = "";
    public string DotNetVersion { get; set; } = "";
    public List<HealthCheckItem> HealthChecks { get; set; } = new();
}

public class HealthCheckItem
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public string Description { get; set; } = "";
    public TimeSpan Duration { get; set; }
}
