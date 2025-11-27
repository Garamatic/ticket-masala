using IT_Project2526;
using IT_Project2526.Models;
using IT_Project2526.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace IT_Project2526.Controllers
{
    [Authorize] // All authenticated users can access tickets
    public class TicketController : Controller
    {
        private readonly ITProjectDB _context;

        public TicketController(ITProjectDB context)
        {
            _context = context;
            /* try
           {
               if (!_context.Database.CanConnect())
               {
                   throw new Exception("Fatal Error: No Database Connection Possible");
               }
           }
           catch (Exception ex) 
           {
               Console.WriteLine(ex.Message);
               throw;
           }*/
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var tickets = await _context.Tickets
                    .AsNoTracking()
                    .Include(t => t.Customer)
                    .Include(t => t.Responsible)
                    .Select(t => new TicketViewModel
                    {
                        Guid = t.Guid,
                        Description = t.Description,
                        TicketStatus = t.TicketStatus,
                        CreationDate = t.CreationDate,
                        CompletionTarget = t.CompletionTarget,
                        ResponsibleName = t.Responsible != null
                            ? $"{t.Responsible.FirstName} {t.Responsible.LastName}"
                            : "Not Assigned",
                        CustomerName = t.Customer != null
                            ? $"{t.Customer.FirstName} {t.Customer.LastName}"
                            : "Unknown"
                    })
                    .ToListAsync();

                return View(tickets);
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }

        public async Task<IActionResult> Detail(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                         .Include(p => p.Tasks)
                         .FirstOrDefaultAsync(p => p.Tasks.Any(t => t.Guid == id));

            var ticket = await _context.Tickets
                        .Include(t => t.Customer)
                        .Include(t => t.Responsible)
                        .Include(t => t.ParentTicket)
                        .Include(t => t.SubTickets)
                        .FirstOrDefaultAsync(m => m.Guid == id);

            if (ticket == null)
            { 
                return NotFound(); 
            }

            var viewModel = new TicketViewModel
            {
                Guid = ticket.Guid,
                Description = ticket.Description,
                TicketStatus = ticket.TicketStatus,
                CreationDate = ticket.CreationDate,
                CompletionTarget = ticket.CompletionTarget,
                Comments = ticket.Comments,
                ResponsibleName = ticket.Responsible != null
                                    ? $"{ticket.Responsible.FirstName} {ticket.Responsible.LastName}"
                                    : "Not Assigned",
                CustomerName = $"{ticket.Customer.FirstName} {ticket.Customer.LastName}",

                ParentTicketGuid = ticket.ParentTicket?.Guid,

                ProjectGuid = project?.Guid ?? Guid.Empty,

                SubTickets = ticket.SubTickets.Select(st => new SubTicketInfo
                    {
                        Guid = st.Guid,
                        Description = st.Description,
                        TicketStatus = st.TicketStatus
                    }).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets
                                        .Include(t => t.Responsible)
                                        .FirstOrDefaultAsync(t => t.Guid == id);

            if (ticket == null) return NotFound();

            // Haal alle mogelijke verantwoordelijke gebruikers op voor de dropdown
            var responsibleUsers = await _context.Users.ToListAsync();

            // Map de databasegegevens naar het ViewModel
            var viewModel = new EditTicketViewModel
            {
                Guid = ticket.Guid,
                Description = ticket.Description,
                TicketStatus = ticket.TicketStatus,
                CompletionTarget = ticket.CompletionTarget,
                ResponsibleUserId = ticket.Responsible?.Id, // ID van de huidige verantwoordelijke

                // Vul de dropdown lijst
                ResponsibleUsers = responsibleUsers.Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = $"{u.FirstName} {u.LastName}"
                }).ToList()
            };

            return View(viewModel);
            
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EditTicketViewModel viewModel)
        {
            if (id != viewModel.Guid) return NotFound();

            if (ModelState.IsValid)
            {
                var ticketToUpdate = await _context.Tickets.FirstOrDefaultAsync(t => t.Guid == id);
                if (ticketToUpdate == null) return NotFound();

                // Werk de eigenschappen bij op basis van het ViewModel
                ticketToUpdate.Description = viewModel.Description;
                ticketToUpdate.TicketStatus = viewModel.TicketStatus;
                ticketToUpdate.CompletionTarget = viewModel.CompletionTarget;
                // Update de verantwoordelijke (u moet nog logica hebben om ApplicationUser te vinden op basis van de Guid/Id)

                try
                {
                    _context.Update(ticketToUpdate);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Detail), new { id = ticketToUpdate.Guid }); 
                }
                catch (DbUpdateConcurrencyException)
                {
                    
                    throw;
                }
            }

            // Als validatie faalt, herlaad de dropdowns en toon de view opnieuw
            viewModel.ResponsibleUsers = await _context.Users.Select(u => new SelectListItem { Value = u.Id.ToString(), Text = $"{u.FirstName} {u.LastName}" }).ToListAsync();
            return View(viewModel);
        }
    }
}
