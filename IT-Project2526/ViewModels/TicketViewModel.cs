using System;
using IT_Project2526.Models;

namespace IT_Project2526.ViewModels
{
    public class TicketViewModel
    {
        public Guid Guid { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ResponsibleName { get; set; }
        public int CommentsCount { get; set; }
        public DateTime? CompletionTarget { get; set; }

    }
}
