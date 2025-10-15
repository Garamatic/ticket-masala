using System.ComponentModel;

namespace IT_Project2526.Models
{
    public class Ticket : BaseModel
    {
        public DateTime? CompletionDate { get; set; }
        public List<Ticket> SubTickets { get; set; } = [];
        public Ticket? ParentTicket { get; set; }
        public required string Description { get; set; }
        public required Status TicketStatus { get; set; } = Status.Pending;
        public required Category Category { get; set; } = Category.Unknown;
        public required SubCategory SubCategory { get; set; } = SubCategory.Unknown;
        public List<Resource> Resources { get; set; } = [];
    }
}
