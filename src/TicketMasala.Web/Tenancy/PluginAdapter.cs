using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TicketMasala.Web.Tenancy;

/// <summary>
/// Standardized plugin interface for interoperability with various plugin-based systems.
/// This interface provides a common contract that can be adapted to different plugin ecosystems.
/// </summary>
public interface IStandardPlugin
{
    /// <summary>
    /// Unique identifier for the plugin (e.g. "TicketMasala.Default", "CustomModule.Reporting").
    /// </summary>
    string PluginId { get; }

    /// <summary>
    /// Human-readable name of the plugin.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Registers services into the DI container.
    /// </summary>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
}

/// <summary>
/// Adapter to bridge Ticket Masala's ITenantPlugin interface with the standardized IStandardPlugin interface.
/// This enables Ticket Masala plugins to work with various plugin-based systems and frameworks.
/// </summary>
public class PluginAdapter : IStandardPlugin
{
    private readonly ITenantPlugin _tenantPlugin;

    public PluginAdapter(ITenantPlugin tenantPlugin)
    {
        _tenantPlugin = tenantPlugin ?? throw new ArgumentNullException(nameof(tenantPlugin));
    }

    /// <summary>
    /// Gets the plugin ID from the tenant ID.
    /// </summary>
    public string PluginId => $"TicketMasala.{_tenantPlugin.TenantId}";

    /// <summary>
    /// Gets the display name from the tenant plugin.
    /// </summary>
    public string DisplayName => _tenantPlugin.DisplayName;

    /// <summary>
    /// Delegates service configuration to the tenant plugin.
    /// </summary>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        _tenantPlugin.ConfigureServices(services, configuration);
    }

    /// <summary>
    /// Gets the underlying tenant plugin for middleware configuration.
    /// </summary>
    public ITenantPlugin TenantPlugin => _tenantPlugin;
}

/// <summary>
/// Extension methods for converting between plugin interfaces.
/// </summary>
public static class PluginAdapterExtensions
{
    /// <summary>
    /// Converts an ITenantPlugin to an IStandardPlugin via adapter.
    /// </summary>
    public static IStandardPlugin ToStandardPlugin(this ITenantPlugin tenantPlugin)
    {
        return new PluginAdapter(tenantPlugin);
    }

    /// <summary>
    /// Converts a collection of ITenantPlugin to IStandardPlugin.
    /// </summary>
    public static IEnumerable<IStandardPlugin> ToStandardPlugins(this IEnumerable<ITenantPlugin> tenantPlugins)
    {
        return tenantPlugins.Select(p => p.ToStandardPlugin());
    }
}

