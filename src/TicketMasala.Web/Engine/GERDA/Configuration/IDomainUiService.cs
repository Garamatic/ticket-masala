using System.Collections.Generic;

namespace TicketMasala.Web.Engine.GERDA.Configuration;

/// <summary>
/// Service for domain-aware UI elements (labels, icons, styles).
/// </summary>
public interface IDomainUiService
{
    /// <summary>
    /// Gets a localized label for the current or specified domain.
    /// Falls back to the global/default if not found.
    /// </summary>
    string GetLabel(string key, string? domainId = null);

    /// <summary>
    /// Gets an icon class (e.g., Bootstrap Icons) for the current or specified domain.
    /// </summary>
    string GetIcon(string key, string? domainId = null);

    /// <summary>
    /// Gets domain-specific CSS classes for branding.
    /// </summary>
    string GetDomainCssClass(string? domainId = null);

    /// <summary>
    /// Gets the current active domain ID for the UI session.
    /// </summary>
    string GetCurrentDomainId();

    /// <summary>
    /// Sets the current active domain ID for the UI session.
    /// </summary>
    void SetCurrentDomainId(string domainId);
}
