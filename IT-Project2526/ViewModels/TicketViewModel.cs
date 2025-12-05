using IT_Project2526;
using IT_Project2526.Models;
using System;

namespace IT_Project2526.ViewModels
{
    public class TicketViewModel
    {
        public Guid Guid { get; set; }
        public Status TicketStatus { get; set; }
        public string Description { get; set; } = string.Empty;

        // Add string Status property for backward compatibility with views
        public string Status => TicketStatus.ToString();

        public DateTime CreationDate { get; set; }
        public DateTime? CompletionTarget { get; set; }
        public string ResponsibleName { get; set; }
        public string CustomerName { get; set; }
        public List<string> Comments { get; set; } = new List<string>();

        public Guid? ParentTicketGuid { get; set; }
        public Guid? ProjectGuid { get; set; }
        public List<SubTicketInfo> SubTickets { get; set; } = new List<SubTicketInfo> { };

        
    }

    public class SubTicketInfo
    {
        public Guid Guid { get; set; }
        public Status TicketStatus { get; set; }
        public string Description { get; set; }
    }
}
