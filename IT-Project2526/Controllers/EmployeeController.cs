using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IT_Project2526.Utilities;

namespace IT_Project2526.Controllers
{
    [Authorize(Roles = Constants.RoleEmployee + "," + Constants.RoleAdmin)]
    public class EmployeeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
