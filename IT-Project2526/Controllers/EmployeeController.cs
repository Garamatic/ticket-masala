using Microsoft.AspNetCore.Mvc;

namespace IT_Project2526.Controllers
{
    public class EmployeeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
