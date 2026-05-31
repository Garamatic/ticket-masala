namespace IT_Project2526.ViewModels
{
    public class ProjectOverviewItemViewModel
    {
        public Guid Guid { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int ProgressPercent { get; set; } // 0..100
        public string Status { get; set; } = string.Empty;
        public DateTime? CompletionTarget { get; set; }
    }
}