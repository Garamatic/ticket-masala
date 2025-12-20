using System.Collections.Generic;

namespace TicketMasala.Web.ViewModels.Projects;

public class ProjectOverviewViewModel
{
    public string Title { get; set; } = "My Projects";
    public List<ProjectOverviewItemViewModel> Projects { get; set; } = new();
    public bool CanCreateProject { get; set; }
}
