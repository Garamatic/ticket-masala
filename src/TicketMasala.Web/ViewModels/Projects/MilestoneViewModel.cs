namespace TicketMasala.Web.ViewModels.Projects;

public class MilestoneViewModel
{
    public DateTime? DueDate { get; set; }
    public int ProgressPercent { get; set; }
    public string Status { get; set; } = string.Empty;
}
