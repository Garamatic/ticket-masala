using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IT_Project2526.Models
{
    public class Project
    {
        public Status Status { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required Employee ProjectManager { get; set; }
    }
}
