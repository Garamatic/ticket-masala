using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TicketMasala.Web.Utilities;

namespace TicketMasala.Web.Models;
    public class ApplicationUser : IdentityUser 
    {
        [ProtectedPersonalData]
        [Required(ErrorMessage = "First name is required")]
        [NoHtml(ErrorMessage = "First name cannot contain HTML")]
        [SafeStringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public required string FirstName { get; set; }
        
        [ProtectedPersonalData]
        [Required(ErrorMessage = "Last name is required")]
        [NoHtml(ErrorMessage = "Last name cannot contain HTML")]
        [SafeStringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public required string LastName { get; set; }
        
        [ProtectedPersonalData]
        [Phone(ErrorMessage = "Invalid phone number")]
        [SafeStringLength(50, ErrorMessage = "Phone number cannot exceed 50 characters")]
        public required string? Phone { get; set; }


        [ProtectedPersonalData]
        [DisplayName("Name")]
        [NotMapped]
        public string Name => $"{FirstName} {LastName}".Trim();
    }

    public class Guest : ApplicationUser { }
    public  class Employee : ApplicationUser
    {
        [Required(ErrorMessage = "Team is required")]
        [NoHtml(ErrorMessage = "Team cannot contain HTML")]
        [SafeStringLength(100, ErrorMessage = "Team cannot exceed 100 characters")]
        public required string Team { get; set; }
        
        public required EmployeeType Level { get; set; }
        
        // GERDA AI Fields
        /// <summary>
        /// Primary language(s) spoken by the agent (e.g., "NL", "FR", "EN" or "NL,FR")
        /// Used for language-based ticket matching in Dispatching service
        /// </summary>
        [SafeStringLength(50, ErrorMessage = "Language cannot exceed 50 characters")]
        public string? Language { get; set; }
        
        /// <summary>
        /// JSON array of specializations/expertise areas (e.g., ["Tax Law", "Fraud Detection"])
        /// Used for category-based affinity matching in Dispatching service
        /// </summary>
        [SafeJson(ErrorMessage = "Specializations must be valid JSON")]
        [SafeStringLength(1000, ErrorMessage = "Specializations cannot exceed 1000 characters")]
        public string? Specializations { get; set; }
        
        /// <summary>
        /// Maximum effort points this agent can handle concurrently
        /// Used for workload balancing in Dispatching and capacity forecasting in Anticipation
        /// Default: 40 points (e.g., 8 tickets × 5 points average)
        /// </summary>
        [Range(1, 200, ErrorMessage = "Max capacity must be between 1 and 200 points")]
        public int MaxCapacityPoints { get; set; } = 40;
        
        /// <summary>
        /// Geographic region or office location (e.g., "Brussels HQ", "Ghent Office")
        /// Used for geographic affinity matching in Dispatching service
        /// </summary>
        [SafeStringLength(100, ErrorMessage = "Region cannot exceed 100 characters")]
        public string? Region { get; set; }

        [PersonalData]
        public string? ProfilePicturePath { get; set; }

        // Department deleted
    }
    // Customer deleted
