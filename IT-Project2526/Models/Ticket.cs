using System.ComponentModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Identity.Client;

namespace IT_Project2526.Models
{
    public class Ticket : BaseModel
    {
        public required Status TicketStatus { get; set; } = Status.Pending;
        public TicketType? TicketType { get; set; }
        public required string Description { get; set; }
        public DateTime? CompletionTarget { get; set; }
        public DateTime? CompletionDate { get; set; }
    

        public Ticket? ParentTicket { get; set; }
        public List<Ticket> SubTickets { get; set; } = [];
        public IdentityUser? Responsible { get; set; }
        public List<IdentityUser> Watchers { get; set; } = [];
        public required Customer Customer { get; set; }

        public List<string> Comments { get; set; } = [];

        public bool PostPoned => CompletionDate > DateTime.UtcNow;
    }
}
