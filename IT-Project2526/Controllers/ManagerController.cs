using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using IT_Project2526.ViewModels;
using IT_Project2526.Models;
using IT_Project2526.Utilities;

namespace IT_Project2526.Controllers
{
    [Authorize(Roles = Constants.RoleAdmin)]
    public class ManagerController : Controller
    {
        private readonly ITProjectDB _context;
        private readonly ILogger<ManagerController> _logger;

        public ManagerController(ITProjectDB context, ILogger<ManagerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Projects()
        {
            try
            {
                _logger.LogInformation("Manager viewing all projects");
                
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading manager projects view");
                return StatusCode(500);
            }
        }
    }
}