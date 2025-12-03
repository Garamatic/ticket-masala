using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using IT_Project2526.Utilities;

namespace IT_Project2526.Models
{
    public class Ticket : BaseModel
    {
        public required Status TicketStatus { get; set; } = Status.Pending;
        public TicketType? TicketType { get; set; }
        
        [Required(ErrorMessage = "Description is required")]
        [NoHtml(ErrorMessage = "Description cannot contain HTML")]
        [SafeStringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
        public required string Description { get; set; }
        
        public DateTime? CompletionTarget { get; set; }
        public DateTime? CompletionDate { get; set; }

        // GERDA AI fields
        public int EstimatedEffortPoints { get; set; } = 0;
        public double PriorityScore { get; set; } = 0.0;
        
        [SafeStringLength(1000, ErrorMessage = "Tags cannot exceed 1000 characters")]
        public string? GerdaTags { get; set; } // Comma-separated: "AI-Dispatched,Spam-Cluster"

        public Ticket? ParentTicket { get; set; }
        public Guid? ParentTicketGuid { get; set; }
        public List<Ticket> SubTickets { get; set; } = [];
        public ApplicationUser? Responsible { get; set; }
        public string? ResponsibleId { get; set; }
        public List<ApplicationUser> Watchers { get; set; } = [];
        public required Customer Customer { get; set; }
        public string? CustomerId { get; set; }
        public Project? Project { get; set; }
        public Guid? ProjectGuid { get; set; }

        public List<string> Comments { get; set; } = [];
     }
}