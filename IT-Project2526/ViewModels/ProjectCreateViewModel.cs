using System.ComponentModel.DataAnnotations;

namespace IT_Project2526.ViewModels
{
    public class ProjectCreateViewModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Description { get; set; } = string.Empty;
        public DateTime? CompletionTarget { get; set; }
        public string? ProjectManagerId { get; set; }
    }
}