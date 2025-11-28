// BFMasala/DataModels.cs

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketMasala.Core.DataModels
{
    // Represents a "Project" or "Queue" in the system
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } // e.g., "VAT Disputes", "Customer Support"

        [MaxLength(50)]
        public string Code { get; set; } // e.g., "V24", "CS"

        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key for the Project Manager (User)
        public string ProjectManagerId { get; set; }
        public virtual ApplicationUser ProjectManager { get; set; }

        // Navigation property for tickets within this project
        public virtual ICollection<Ticket> Tickets { get; set; }
    }

    // Represents a "Ticket" or "Case" in the system
    public class Ticket
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Title { get; set; }

        public string Description { get; set; }

        // Generic Requester ID - could be Client ID, Employee ID, etc.
        [Required]
        [MaxLength(255)]
        public string RequesterId { get; set; }

        // Category for classification and complexity estimation
        [Required]
        [MaxLength(255)]
        public string Category { get; set; } // e.g., "Password Reset", "Fraud Investigation"

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } // e.g., "New", "Assigned", "In Progress", "Done"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        // Estimated effort / complexity (Fibonacci points)
        public int EstimatedEffortPoints { get; set; } = 0;

        // Priority score calculated by GERDA (WSJF)
        public double PriorityScore { get; set; } = 0.0;

        // Foreign key for Project
        public int ProjectId { get; set; }
        public virtual Project Project { get; set; }

        // Foreign key for Assignee (User)
        public string AssigneeId { get; set; }
        public virtual ApplicationUser Assignee { get; set; }

        // Tags for GERDA flags (e.g., "AI-Dispatched", "Spam-Cluster", "GERDA-ALERT")
        public ICollection<TicketTag> Tags { get; set; } = new List<TicketTag>();

        // Parent ticket for grouped tickets
        public int? ParentTicketId { get; set; }
        public virtual Ticket ParentTicket { get; set; }
        public virtual ICollection<Ticket> ChildTickets { get; set; }
    }

    // Represents a Tag associated with a Ticket
    public class TicketTag
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        public virtual Ticket Ticket { get; set; }

        [Required]
        [MaxLength(100)]
        public string TagName { get; set; } // e.g., "AI-Dispatched", "Spam-Cluster", "Critical-SLA"
    }

    // Represents a log entry for changes made to a ticket (Audit Trail)
    public class TicketLog
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        public virtual Ticket Ticket { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } // e.g., "Status Change", "Assigned", "Description Updated"

        [MaxLength(255)]
        public string OldValue { get; set; }

        [MaxLength(255)]
        public string NewValue { get; set; }

        [Required]
        [MaxLength(255)]
        public string ChangedByUserId { get; set; } // User ID who made the change
        public virtual ApplicationUser ChangedByUser { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // Represents a snapshot of daily queue statistics for Anticipation (GERDA A)
    public class DailyQueueStats
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }

        public int ProjectId { get; set; } // Link to Project/Queue
        public virtual Project Project { get; set; }

        public int OpenCasesCount { get; set; }
        public int ClosedCasesToday { get; set; }
        public int IncomingCasesToday { get; set; }
    }

    // Placeholder for ApplicationUser (Assuming ASP.NET Core Identity or similar)
    // This class would typically be part of your Identity setup.
    public class ApplicationUser
    {
        [Key]
        public string Id { get; set; } // GUID or similar

        [Required]
        [MaxLength(255)]
        public string UserName { get; set; }

        [Required]
        [MaxLength(255)]
        public string Email { get; set; }

        // Navigation properties for roles, assigned tickets, managed projects, etc.
        public virtual ICollection<Ticket> AssignedTickets { get; set; }
        public virtual ICollection<Project> ManagedProjects { get; set; }
        public virtual ICollection<TicketLog> TicketLogs { get; set; }

        // Agent-specific properties for capacity planning (GERDA A)
        public double AverageDailyTicketsClosed { get; set; } // Velocity
        public bool IsAvailable { get; set; } = true; // For simple availability check
        public DateTime? LastDayOff { get; set; } // Example for more complex availability
    }
}
