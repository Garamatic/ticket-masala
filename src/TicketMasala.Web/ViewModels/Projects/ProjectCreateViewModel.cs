using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Web.ViewModels.Projects;

    public class ProjectCreateViewModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Description { get; set; } = string.Empty;
        public DateTime? CompletionTarget { get; set; }
        public string? ProjectManagerId { get; set; }
}
