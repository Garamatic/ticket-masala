using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using IT_Project2526.Models;
using IT_Project2526;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using IT_Project2526.ViewModels;

namespace IT_Project2526.Controllers
{
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

        public async Task<IActionResult> Detail(Guid? guid)
        {
            if (guid == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                         .Include(p => p.Tasks)
                         .FirstOrDefaultAsync(p => p.Tasks.Any(t => t.Guid == guid));

            var ticket = await _context.Tickets
                        .Include(t => t.Customer)
                        .Include(t => t.Responsible)
                        .Include(t => t.ParentTicket)
                        .Include(t => t.SubTickets)
                        .FirstOrDefaultAsync(m => m.Guid == guid);

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
                        Description = st.Description
                    }).ToList()
            };

            return View(viewModel);
        }
    }
}
