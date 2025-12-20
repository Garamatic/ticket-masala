using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace TicketMasala.Web.Tenancy;

/// <summary>
/// Loads tenant plugins from external assemblies.
/// Plugins can register custom services, strategies, and middleware.
/// </summary>
public static class TenantPluginLoader
{
    private static readonly List<ITenantPlugin> _loadedPlugins = new();
    
    /// <summary>
    /// Get all loaded tenant plugins.
    /// </summary>
    public static IReadOnlyList<ITenantPlugin> LoadedPlugins => _loadedPlugins.AsReadOnly();
    
    /// <summary>
    /// Load plugins from the specified directory and register their services.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="pluginPath">Path to directory containing plugin DLLs.</param>
    public static void LoadPlugins(WebApplicationBuilder builder, string pluginPath)
    {
        if (string.IsNullOrEmpty(pluginPath) || !Directory.Exists(pluginPath))
        {
            Console.WriteLine($"[Tenancy] Plugin path not found or empty: {pluginPath}");
            return;
        }
        
        Console.WriteLine($"[Tenancy] Loading plugins from: {pluginPath}");
        
        foreach (var dllPath in Directory.GetFiles(pluginPath, "*.dll"))
        {
            try
            {
                LoadPluginAssembly(builder, dllPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Tenancy] Failed to load plugin {Path.GetFileName(dllPath)}: {ex.Message}");
            }
        }
        
        Console.WriteLine($"[Tenancy] Loaded {_loadedPlugins.Count} plugin(s)");
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
            
            Console.WriteLine($"[Tenancy] Registering plugin: {plugin.DisplayName} ({plugin.TenantId})");
            
            // Let the plugin register its services
            plugin.ConfigureServices(builder.Services, builder.Configuration);
            
            _loadedPlugins.Add(plugin);
        }
    }
    
    /// <summary>
    /// Configure middleware for all loaded plugins.
    /// Call this after standard middleware is configured.
    /// </summary>
    public static void ConfigurePluginMiddleware(IApplicationBuilder app, IWebHostEnvironment env)
    {
        foreach (var plugin in _loadedPlugins)
        {
            try
            {
                plugin.ConfigureMiddleware(app, env);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Tenancy] Failed to configure middleware for {plugin.TenantId}: {ex.Message}");
            }
        }
    }
}
