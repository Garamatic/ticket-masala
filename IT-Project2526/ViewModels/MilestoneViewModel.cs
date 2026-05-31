namespace IT_Project2526.ViewModels
{
    public class MilestoneViewModel
    {
        public Guid Guid { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public int ProgressPercent { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
