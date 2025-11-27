using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace IT_Project2526.Controllers
{
    [Authorize] // All authenticated users can access tickets
    public class TicketController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
