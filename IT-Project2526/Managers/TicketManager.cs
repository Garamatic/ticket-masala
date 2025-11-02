using System;
using System.Collections.Generic;
using System.Linq;
using IT_Project2526.Models;

namespace IT_Project2526.Managers
{
    public class TicketManager
    {
        private readonly ITProjectDB db;

        public TicketManager(ITProjectDB db)
        {
            this.db = db;
        }

        public Ticket? FetchTicket(Guid ticketGuid) 
        {
            return Valid().FirstOrDefault(x => x.Guid == ticketGuid);
        }

        /*public void CreateTicket(TicketVM vm)
        {
            if (vm != null)
            {
                Ticket ticket = new Ticket()
                {
                    Customer = vm.Customer,
                    TicketStatus = Status.Pending,
                    Description = vm.Description,
                };
                if (vm.ParentTicket != null)
                {
                    ticket.ParentTicket = vm.ParentTicket;
                }
            } 
        }
        public void UpdateTicket(TicketVM vm)
        {
            if (vm != null)
            {
                var ticket = Valid().FirstOrDefault(x => x.Guid == vm.Guid) ?? throw new Exception("Ticket not found"); 
                ticket.TicketStatus = vm.TicketStatus;
                ticket.Description = vm.Description;
                ticket.ParentTicket = vm.ParentTicket;
                ticket.Comments = vm.Comments;
                ticket.Watchers = vm.Watchers;
                ticket.Responsible = vm.Responsible;
                ticket.CompletionDate = vm.CompletionDate;
                ticket.SubTickets = vm.SubTickets;
          
            }
        }*/
        public void ChangeTicketStatus(Guid ticketGuid,Status status)
        {
            var ticket = Valid().FirstOrDefault(x => x.Guid == ticketGuid) ?? throw new Exception("Ticket not found");
            ticket.TicketStatus = status;
            db.SaveChanges();
        }
        public void CompleteTicket(Guid ticketGuid)
        {
            var ticket = Valid().FirstOrDefault(x => x.Guid == ticketGuid) ?? throw new Exception("Ticket not found");
            ticket.TicketStatus = Status.Completed;
            ticket.ValidUntil = DateTime.Now;
            db.SaveChanges();
        }
        public void FailTicket(Guid ticketGuid)
        {
            var ticket = Valid().FirstOrDefault(x => x.Guid == ticketGuid) ?? throw new Exception("Ticket not found");
            ticket.TicketStatus = Status.Failed;
            ticket.ValidUntil = DateTime.Now;
            db.SaveChanges();
        }
        public List<Ticket> PostponedTickets()
        {
            // Avoid referencing a non-existent enum member by comparing the enum name string.
            return Valid().Where(x => x.TicketStatus.ToString() == "Postponed").ToList();
        }
        public List<Ticket> PendingTickets()
        {
            return Valid().Where(x => x.TicketStatus == Status.Pending).ToList();
        }
        public List<Ticket> RejectedTickets()
        {
            return Valid().Where(x => x.TicketStatus == Status.Rejected).ToList();
        }
        public List<Ticket> AssignedTickets()
        {
            return Valid().Where(x => x.TicketStatus == Status.Assigned).ToList();
        }
        public List<Ticket> InProgressTickets()
        {
            return Valid().Where(x => x.TicketStatus == Status.InProgress).ToList();
        }
        public List<Ticket> CompletedTickets()
        {
            return Valid().Where(x => x.TicketStatus == Status.Completed).ToList();
        }
        public List<Ticket> FailedTicket()
        {
            return Valid().Where(x => x.TicketStatus == Status.Rejected).ToList();
        }
        public List<Ticket> Valid()
        {
            return db.Tickets.Where(x => x.ValidUntil == null).ToList();
        }
        public List<Ticket> TicketsForCustomer(string customerCode)
        {
            return Valid().Where(x => x.Customer.Code == customerCode).ToList();
        }
        public List<Ticket> ResponsibleForTickets(string name)
        {
            return Valid().Where(x => x.Responsible is not null && (x.Responsible as ApplicationUser)?.Name == name).ToList();
        }
        public List<Ticket> WatchingForTickets(string name)
        {
            return Valid().Where(x => x.Watchers.Any(y => (y as ApplicationUser)?.Name == name)).ToList();
        }
    }
}
