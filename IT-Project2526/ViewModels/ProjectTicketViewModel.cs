using System;
using System.Collections.Generic;

namespace IT_Project2526.ViewModels
{
    public class ProjectTicketViewModel
    {
        public ProjectViewModel ProjectDetails { get; set; }
        public IReadOnlyList<TicketViewModel> Tasks { get; set; } = Array.Empty<TicketViewModel>();
    }
}
