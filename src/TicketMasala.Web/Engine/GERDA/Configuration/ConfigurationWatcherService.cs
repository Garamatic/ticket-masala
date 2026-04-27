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
        // Fire and forget with proper async handling
        _ = HandleConfigChangeAsync(e);
    }

    private async Task HandleConfigChangeAsync(FileSystemEventArgs e)
    {
        // Debounce: wait for file write to complete
        await Task.Delay(500);

        try
        {
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
