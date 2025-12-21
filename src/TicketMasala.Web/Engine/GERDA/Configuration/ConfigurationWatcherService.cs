using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicketMasala.Web.Configuration;

namespace TicketMasala.Web.Engine.GERDA.Configuration;

/// <summary>
/// Watches for changes to configuration files and triggers reloads.
/// </summary>
public class ConfigurationWatcherService : BackgroundService
{
    private readonly IDomainConfigurationService _domainConfigurationService;
    private readonly ILogger<ConfigurationWatcherService> _logger;
    private readonly string _configBasePath;
    private FileSystemWatcher? _watcher;

    public ConfigurationWatcherService(
        IDomainConfigurationService domainConfigurationService,
        IWebHostEnvironment environment,
        ILogger<ConfigurationWatcherService> logger)
    {
        _domainConfigurationService = domainConfigurationService;
        _logger = logger;
        _configBasePath = ConfigurationPaths.GetConfigBasePath(environment.ContentRootPath);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configFile = "masala_config.json";
        var fullPath = Path.Combine(_configBasePath, configFile);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Configuration file {Path} not found. Watcher disabled.", fullPath);
            return Task.CompletedTask;
        }

        _logger.LogInformation("Starting configuration watcher for {Path}", fullPath);

        _watcher = new FileSystemWatcher(_configBasePath, configFile)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Renamed += OnChanged;

        return Task.CompletedTask;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce logic could be added here if needed, but for now simple reload.
        // FileSystemWatcher often fires multiple events for a single save.
        // We catch exceptions to avoid crashing the service.
        try
        {
            // Small delay to ensure file write is complete
            Thread.Sleep(500);

            _logger.LogInformation("Configuration change detected: {ChangeType} {FullPath}", e.ChangeType, e.FullPath);
            _domainConfigurationService.ReloadConfiguration();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling configuration change");
        }
    }

    public override void Dispose()
    {
        _watcher?.Dispose();
        base.Dispose();
    }
}
