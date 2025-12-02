using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT_Project2526.Models
{
    public class ApplicationUser : IdentityUser 
    {
        [ProtectedPersonalData]
        public required string FirstName { get; set; }
        [ProtectedPersonalData]
        public required string LastName { get; set; }
        [ProtectedPersonalData]
        public required string? Phone { get; set; }


        [ProtectedPersonalData]
        [DisplayName("Name")]
        [NotMapped]
        public string Name => $"{FirstName} {LastName}".Trim();
    }

    public class Guest : ApplicationUser { }
    public  class Employee : ApplicationUser
    {
        public required string Team { get; set; }
        public required EmployeeType Level { get; set; }
        
        // GERDA AI Fields
        /// <summary>
        /// Primary language(s) spoken by the agent (e.g., "NL", "FR", "EN" or "NL,FR")
        /// Used for language-based ticket matching in Dispatching service
        /// </summary>
        public string? Language { get; set; }
        
        /// <summary>
        /// JSON array of specializations/expertise areas (e.g., ["Tax Law", "Fraud Detection"])
        /// Used for category-based affinity matching in Dispatching service
        /// </summary>
        public string? Specializations { get; set; }
        
        /// <summary>
        /// Maximum effort points this agent can handle concurrently
        /// Used for workload balancing in Dispatching and capacity forecasting in Anticipation
        /// Default: 40 points (e.g., 8 tickets × 5 points average)
        /// </summary>
        public int MaxCapacityPoints { get; set; } = 40;
        
        /// <summary>
        /// Geographic region or office location (e.g., "Brussels HQ", "Ghent Office")
        /// Used for geographic affinity matching in Dispatching service
        /// </summary>
        public string? Region { get; set; }
    }
    public class Customer : ApplicationUser
    {
        public string? Code { get; set; }
        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
