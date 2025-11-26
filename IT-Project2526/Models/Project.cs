using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IT_Project2526.Models
{
    public class Project : BaseModel
    {
        public Status Status { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        // Make ProjectManager optional to allow creating projects before assignment
        public Employee? ProjectManager { get; set; }
        public string? ProjectManagerId { get; set; }

        public DateTime? CompletionTarget { get; set; }
        public DateTime? CompletionDate { get; set; }

        public List<Ticket> Tasks { get; set; } = [];
        public List<Resource> Resources { get; set; } = [];
    }
}
