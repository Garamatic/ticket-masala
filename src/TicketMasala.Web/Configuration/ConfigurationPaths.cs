namespace TicketMasala.Web.Configuration;

using System.Collections.Concurrent;

/// <summary>
/// Centralized configuration path resolution for Ticket Masala.
/// Supports deployment patterns via MASALA_CONFIG_PATH environment variable.
/// </summary>
public static class ConfigurationPaths
{
    private static readonly ConcurrentDictionary<string, string> _configBasePathByContentRoot =
        new(GetPathComparer());

    private static StringComparer GetPathComparer() =>
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    /// <summary>
    /// Gets the base configuration directory path.
    /// Resolution order:
    /// 1. MASALA_CONFIG_PATH environment variable (Pro deployment)
    /// 2. /app/config (Docker container default)
    /// 3. ../../config relative to ContentRootPath (Development)
    /// </summary>
    public static string GetConfigBasePath(string contentRootPath)
    {
        var normalizedContentRootPath = Path.GetFullPath(contentRootPath);

        if (_configBasePathByContentRoot.TryGetValue(normalizedContentRootPath, out var cached))
        {
            return cached;
        }

        // Check environment variable first (Pro deployment override)
        var envPath = Environment.GetEnvironmentVariable("MASALA_CONFIG_PATH");
        if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
        {
            _configBasePathByContentRoot[normalizedContentRootPath] = envPath;
            return envPath;
        }

        // Docker container default
        if (Directory.Exists("/app/config"))
        {
            _configBasePathByContentRoot[normalizedContentRootPath] = "/app/config";
            return "/app/config";
        }

        // Development fallback - Local config (Priority)
        var localConfig = Path.Combine(normalizedContentRootPath, "config");
        if (Directory.Exists(localConfig))
        {
            _configBasePathByContentRoot[normalizedContentRootPath] = localConfig;
            return localConfig;
        }

        // Development fallback - Standard Repo Structure (Legacy)
        var legacyRepoConfig = Path.Combine(normalizedContentRootPath, "..", "..", "config");
        _configBasePathByContentRoot[normalizedContentRootPath] = legacyRepoConfig;
        return legacyRepoConfig;
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
        _configBasePathByContentRoot.Clear();
    }
}
