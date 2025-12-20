namespace TicketMasala.Web.Configuration;

/// <summary>
/// Centralized configuration path resolution for Ticket Masala.
/// Supports deployment patterns via MASALA_CONFIG_PATH environment variable.
/// </summary>
public static class ConfigurationPaths
{
    private static string? _configBasePath;
    
    /// <summary>
    /// Gets the base configuration directory path.
    /// Resolution order:
    /// 1. MASALA_CONFIG_PATH environment variable (Pro deployment)
    /// 2. /app/config (Docker container default)
    /// 3. ../../config relative to ContentRootPath (Development)
    /// </summary>
    public static string GetConfigBasePath(string contentRootPath)
    {
        if (_configBasePath != null)
            return _configBasePath;
            
        // Check environment variable first (Pro deployment override)
        var envPath = Environment.GetEnvironmentVariable("MASALA_CONFIG_PATH");
        if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
        {
            _configBasePath = envPath;
            return _configBasePath;
        }
        
        // Docker container default
        if (Directory.Exists("/app/config"))
        {
            _configBasePath = "/app/config";
            return _configBasePath;
        }
        
        // Development fallback
        _configBasePath = Path.Combine(contentRootPath, "..", "..", "config");
        return _configBasePath;
    }
    
    /// <summary>
    /// Gets the full path to a configuration file.
    /// </summary>
    public static string GetConfigFilePath(string contentRootPath, string fileName)
    {
        var basePath = GetConfigBasePath(contentRootPath);
        return Path.Combine(basePath, fileName);
    }
    
    /// <summary>
    /// Resets the cached config path (useful for testing).
    /// </summary>
    public static void ResetCache()
    {
        _configBasePath = null;
    }
}
