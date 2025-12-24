using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace TicketMasala.Web.Engine.GERDA.Configuration;

public class DomainUiService : IDomainUiService
{
    private readonly IDomainConfigurationService _configService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string DomainCookieName = "MasalaDomain";

    public DomainUiService(
        IDomainConfigurationService configService,
        IHttpContextAccessor httpContextAccessor)
    {
        _configService = configService;
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCurrentDomainId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return _configService.GetDefaultDomainId();

        // 1. Try cookie
        if (context.Request.Cookies.TryGetValue(DomainCookieName, out var cookieDomainId))
        {
            return cookieDomainId;
        }

        // 2. Default
        return _configService.GetDefaultDomainId();
    }

    public void SetCurrentDomainId(string domainId)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return;

        context.Response.Cookies.Append(DomainCookieName, domainId, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = true,
            SameSite = SameSiteMode.Strict
        });
    }

    public string GetLabel(string key, string? domainId = null)
    {
        domainId ??= GetCurrentDomainId();
        var domain = _configService.GetDomain(domainId);

        // Try to find in UI configuration if it exists
        // (Note: We might need to extend MasalaDomainsConfig to have a generic Labels dictionary per domain)
        // For now, let's look for specific known keys like 'DomainName'
        if (key == "DomainName") return domain?.Ui.Label ?? domainId;

        return key; // Fallback
    }

    public string GetIcon(string key, string? domainId = null)
    {
        domainId ??= GetCurrentDomainId();
        var domain = _configService.GetDomain(domainId);

        if (key == "DomainIcon") return domain?.Ui.Icon ?? "bi-grid";

        return "bi-circle";
    }

    public string GetDomainCssClass(string? domainId = null)
    {
        domainId ??= GetCurrentDomainId();
        var domain = _configService.GetDomain(domainId);
        return domain?.Ui.CssClass ?? "theme-default";
    }
}
