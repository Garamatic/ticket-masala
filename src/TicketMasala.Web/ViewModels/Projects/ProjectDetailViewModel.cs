namespace TicketMasala.Web.ViewModels.Projects;
using TicketMasala.Web.ViewModels.Tickets;

public class ProjectDetailViewModel
{
        public string? ImageUrl { get; set; }
        public int ProgressPercent { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<MilestoneViewModel> Milestones { get; set; } = new();
        public List<TicketViewModel> Tickets { get; set; } = new();
        public List<string> Resources { get; set; } = new();

    }
