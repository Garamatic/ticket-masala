namespace IT_Project2526.ViewModels
{
    public class ProjectDetailViewModel
    {
        public Guid Guid { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int ProgressPercent { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<MilestoneViewModel> Milestones { get; set; } = new();
        public List<TicketViewModel> Tickets { get; set; } = new();
        public List<string> Resources { get; set; } = new();

    }
}
