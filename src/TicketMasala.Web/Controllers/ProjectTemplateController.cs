using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Data;

namespace TicketMasala.Web.Controllers;

[Authorize(Roles = Constants.RoleAdmin)]
public class ProjectTemplateController : Controller
{
    private readonly MasalaDbContext _context;

    public ProjectTemplateController(MasalaDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.ProjectTemplates.Include(t => t.Tickets).ToListAsync());
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectTemplate template)
    {
        if (ModelState.IsValid)
        {
            template.Guid = Guid.NewGuid();
            _context.Add(template);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(template);
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();

        var template = await _context.ProjectTemplates
            .Include(t => t.Tickets)
            .FirstOrDefaultAsync(m => m.Guid == id);

        if (template == null) return NotFound();
        return View(template);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ProjectTemplate template)
    {
        if (id != template.Guid) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(template);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProjectTemplateExists(template.Guid)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(template);
    }

    // Action to add a ticket to a template
    [HttpPost]
    public async Task<IActionResult> AddTicket(Guid templateId, string description, int effort, Priority priority, TicketType type)
    {
        var template = await _context.ProjectTemplates.FindAsync(templateId);
        if (template == null) return NotFound();

        var ticket = new TemplateTicket
        {
            Guid = Guid.NewGuid(),
            Description = description,
            EstimatedEffortPoints = effort,
            Priority = priority,
            TicketType = type,
            ProjectTemplateId = templateId
        };

        _context.TemplateTickets.Add(ticket);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Edit), new { id = templateId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTicket(Guid id)
    {
        var ticket = await _context.TemplateTickets.FindAsync(id);
        if (ticket != null)
        {
            var templateId = ticket.ProjectTemplateId;
            _context.TemplateTickets.Remove(ticket);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Edit), new { id = templateId });
        }
        return RedirectToAction(nameof(Index));
    }

    private bool ProjectTemplateExists(Guid id)
    {
        return _context.ProjectTemplates.Any(e => e.Guid == id);
    }
}
