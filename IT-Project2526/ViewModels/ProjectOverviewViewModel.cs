using System.Collections.Generic;

namespace IT_Project2526.ViewModels
{
    public class ProjectOverviewViewModel
    {
        public string Title { get; set; } = "My Projects";
        public List<ProjectOverviewItemViewModel> Projects { get; set; } = new();
        public bool CanCreateProject { get; set; }
    }
}