using Microsoft.AspNetCore.Mvc;
using TicketMasala.Web.Engine.GERDA.Configuration;

namespace TicketMasala.Web.Controllers;

public class DomainController : Controller
{
    private readonly IDomainUiService _uiService;

    public DomainController(IDomainUiService uiService)
    {
        _uiService = uiService;
    }

    [HttpPost]
    public IActionResult SetDomain(string domainId, string returnUrl)
    {
        _uiService.SetCurrentDomainId(domainId);

        if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            return RedirectToAction("Index", "Home");
        }

        return Redirect(returnUrl);
    }
}
