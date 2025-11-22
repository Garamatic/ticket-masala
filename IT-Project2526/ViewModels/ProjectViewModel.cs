using IT_Project2526.Models;

namespace IT_Project2526.ViewModels
{
    public class ProjectViewModel
    {
        public Guid Guid { get; set; } 
        public string Name { get; set; }
        public string Description { get; set; }
        public Status Status { get; set; }
        public string ProjectManagerName { get; set; }
        public int TicketCount { get; set; }
        public Guid? CurrentTicketGuid { get; set; }
        public string CurrentTicketDescription { get; set; }
        public Status? CurrentTicketStatus { get; set; }

    }

    public class ProjectDetailsViewModel
    {
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ProjectManagerName { get; set; }
        public Status Status { get; set; }
        public DateTime? CompletionTarget { get; set; }
        public List<ProjectTicketInfo> Tasks { get; set; } = new List<ProjectTicketInfo>();

        public class ProjectTicketInfo
        {
            public Guid Guid { get; set; }
            public string Description { get; set; }
            public Status TicketStatus { get; set; }
        }
    }
}
