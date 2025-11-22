using IT_Project2526;
using IT_Project2526.Models;
using System;
using IT_Project2526.Models;

namespace IT_Project2526.ViewModels
{
    public class TicketViewModel
    {
        public Guid Guid { get; set; }
        public Status TicketStatus { get; set; }
        public DateTime CreationDate { get; set; }
    }
}
