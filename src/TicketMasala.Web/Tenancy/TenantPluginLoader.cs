using System.Reflection;
using Microsoft.Extensions.Logging;

namespace TicketMasala.Web.Tenancy;

/// <summary>
/// Loads tenant plugins from external assemblies.
/// Plugins can register custom services, strategies, and middleware.
/// </summary>
public static class TenantPluginLoader
{
    private static readonly List<ITenantPlugin> _loadedPlugins = new();
    private static readonly Lock _pluginsLock = new();

    /// <summary>
    /// Get all loaded tenant plugins.
    /// </summary>
    public static IReadOnlyList<ITenantPlugin> LoadedPlugins
    {
        get
        {
            lock (_pluginsLock)
            {
                return _loadedPlugins.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Load plugins from the specified directory and register their services.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="pluginPath">Path to directory containing plugin DLLs.</param>
    /// <param name="logger">Optional logger for diagnostic messages.</param>
    public static void LoadPlugins(WebApplicationBuilder builder, string pluginPath, ILogger? logger = null)
    {
        if (string.IsNullOrEmpty(pluginPath) || !Directory.Exists(pluginPath))
        {
            return;
        }

        foreach (var dllPath in Directory.GetFiles(pluginPath, "*.dll"))
        {
            try
            {
                LoadPluginAssembly(builder, dllPath);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to load plugin from {DllPath}", dllPath);
            }
        }
    }

    /// <summary>
    /// Load a single plugin assembly and register its services.
    /// </summary>
    private static void LoadPluginAssembly(WebApplicationBuilder builder, string dllPath)
    {
        var assembly = Assembly.LoadFrom(dllPath);

        var pluginTypes = assembly.GetTypes()
            .Where(t => typeof(ITenantPlugin).IsAssignableFrom(t)
                        && !t.IsInterface
                        && !t.IsAbstract);

        foreach (var pluginType in pluginTypes)
        {
            var plugin = (ITenantPlugin)Activator.CreateInstance(pluginType)!;

            // Plugin registration is logged via ILogger if needed
            // Plugins should handle their own service registration logging

            // Let the plugin register its services
            plugin.ConfigureServices(builder.Services, builder.Configuration);

            lock (_pluginsLock)
            {
                _loadedPlugins.Add(plugin);
            }
        }
    }

    /// <summary>
    /// Configure middleware for all loaded plugins.
    /// Call this after standard middleware is configured.
    /// </summary>
    public static void ConfigurePluginMiddleware(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var logger = app.ApplicationServices.GetService<ILoggerFactory>()?.CreateLogger("TenantPluginLoader");

        lock (_pluginsLock)
        {
            foreach (var plugin in _loadedPlugins)
            {
                try
                {
                    plugin.ConfigureMiddleware(app, env);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to configure middleware for {TenantId}", plugin.TenantId);
                }
            }
        }
    }
}
