using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketMasala.Domain.Common;

namespace TicketMasala.Web.Controllers;

[Authorize(Roles = Constants.RoleEmployee + "," + Constants.RoleAdmin)]
public class EmployeeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
