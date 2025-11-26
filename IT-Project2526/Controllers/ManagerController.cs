using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT_Project2526.ViewModels;
using IT_Project2526.Models;
namespace IT_Project2526.Controllers
{
    public class ManagerController : Controller
    {
        private readonly ITProjectDB _context;
        public ManagerController(ITProjectDB context)
        {
            _context = context;
        }

        public IActionResult Projects()
        {
            var projects = _context.Projects
                .Include(p => p.Tasks)
                .Include(p => p.ProjectManager)
                .Where(p => p.ValidUntil == null)
                .ToList();

            var viewModels = projects.Select(p => new ProjectTicketViewModel
            {
                ProjectDetails = new ProjectViewModel
                {
                    Guid = p.Guid,
                    Name = p.Name,
                    Description = p.Description,
                    Status = p.Status,
                    ProjectManager = p.ProjectManager
                },
                Tasks = p.Tasks.Select(t => new TicketViewModel
                {
                    Guid = t.Guid,
                    Description = t.Description,
                    Status = t.TicketStatus.ToString(),
                    ResponsibleName = t.Responsible?.Name,
                    CommentsCount = t.Comments?.Count ?? 0,
                    CompletionTarget = t.CompletionTarget
                }).ToList()
            }).ToList();

            return View(viewModels);
        }
    }
}