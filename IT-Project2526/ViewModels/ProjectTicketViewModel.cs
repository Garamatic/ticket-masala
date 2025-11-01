using System.Collections.Generic;

namespace IT_Project2526.ViewModels
{
    public class ProjectTicketViewModel
    {
        public ProjectViewModel ProjectDetails { get; set; }
        public List<TicketViewModel> Tasks { get; set; } = new List<TicketViewModel>();
    }
}
