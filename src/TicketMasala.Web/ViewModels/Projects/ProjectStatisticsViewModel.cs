namespace TicketMasala.Web.ViewModels.Projects;

public class ProjectStatisticsViewModel
{
    public int TotalProjects { get; set; }
    public int ActiveProjects { get; set; }
    public int CompletedProjects { get; set; }
    public int PendingProjects { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
}
