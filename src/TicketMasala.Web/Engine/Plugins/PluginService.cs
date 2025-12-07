namespace TicketMasala.Web.Engine.Plugins;

/// <summary>
/// Interface for Masala plugins. Plugins can register custom services,
/// strategies, or middleware at startup time.
/// KISS approach: plugins are discovered at startup, not hot-loaded.
/// </summary>
public interface IMasalaPlugin
{
    /// <summary>
    /// Unique plugin identifier
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Plugin version
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Plugin description
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Register plugin services with the DI container
    /// </summary>
    void RegisterServices(IServiceCollection services, IConfiguration configuration);
}

/// <summary>
/// Service for discovering and loading plugins
/// </summary>
public interface IPluginService
{
    IEnumerable<IMasalaPlugin> LoadedPlugins { get; }
    void LoadPlugins();
}

/// <summary>
/// KISS plugin loader - discovers plugins from assemblies in /plugins folder
/// </summary>
public class PluginService : IPluginService
{
    private readonly ILogger<PluginService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IServiceCollection _services;
    private readonly List<IMasalaPlugin> _loadedPlugins = new();

    public IEnumerable<IMasalaPlugin> LoadedPlugins => _loadedPlugins.AsReadOnly();

    public PluginService(
        ILogger<PluginService> logger,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IServiceCollection services)
    {
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
        _services = services;
    }

    public void LoadPlugins()
    {
        var pluginsPath = Path.Combine(_environment.ContentRootPath, "plugins");
        
        if (!Directory.Exists(pluginsPath))
        {
            _logger.LogInformation("No plugins folder found at {Path}", pluginsPath);
            return;
        }

        var dllFiles = Directory.GetFiles(pluginsPath, "*.dll");
        _logger.LogInformation("Found {Count} potential plugin assemblies", dllFiles.Length);

        foreach (var dllPath in dllFiles)
        {
            try
            {
                var assembly = System.Reflection.Assembly.LoadFrom(dllPath);
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IMasalaPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var pluginType in pluginTypes)
                {
                    var plugin = (IMasalaPlugin?)Activator.CreateInstance(pluginType);
                    if (plugin != null)
                    {
                        plugin.RegisterServices(_services, _configuration);
                        _loadedPlugins.Add(plugin);
                        _logger.LogInformation("Loaded plugin: {Name} v{Version}", plugin.Name, plugin.Version);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin from {Path}", dllPath);
            }
        }

        _logger.LogInformation("Successfully loaded {Count} plugins", _loadedPlugins.Count);
    }
}
