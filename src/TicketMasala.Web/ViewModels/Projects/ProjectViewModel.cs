using TicketMasala.Web.Models;
using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Web.ViewModels.Projects;
    public class ProjectViewModel
    {
       [Required]
       public Guid Guid { get; init; }
       [Required, StringLength(200)]
       public string Name { get; init; } = string.Empty;
       [StringLength(2000)]
       public string Description { get; init; } = string.Empty;
       public Status Status { get; init; }
       public Employee ProjectManager {  get; init; }
       public string ProjectManagerName { get; set; } = string.Empty;
       public int TicketCount { get; set; }

        public class ProjectTicketInfo
        {
            public Guid Guid { get; set; }
            public string Description { get; set; } = string.Empty;
            public Status TicketStatus { get; set; }
        }
}
