using System;
using System.Collections.Generic;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.ViewModels.Projects;
    public class ProjectTicketViewModel
    {
        public ProjectViewModel ProjectDetails { get; set; } = new ProjectViewModel();
        public IReadOnlyList<TicketViewModel> Tasks { get; set; } = Array.Empty<TicketViewModel>();
}
