using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TicketMasala.Domain.Common;

// Backwards-compatibility alias: treat `Customer` as an `ApplicationUser` so
// existing code that declares `Customer` variables can be assigned
// `ApplicationUser` instances without wide-ranging changes.
// using Customer = TicketMasala.Domain.Entities.ApplicationUser; 

namespace TicketMasala.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }


    [ProtectedPersonalData]
    [DisplayName("Name")]
    [NotMapped]
    public string Name => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Alias for Name property - returns FirstName + LastName
    /// </summary>
    [NotMapped]
    public string FullName => Name;

    // Backwards-compatible code used in ingestion and seeding
    [SafeStringLength(50)]
    public string? Code { get; set; }
}

public class Guest : ApplicationUser { }
public class Employee : ApplicationUser
{
    public string Team { get; set; } = string.Empty;

    public EmployeeType Level { get; set; }

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

    // Backwards-compat: some code references DepartmentId on Employee
    [SafeStringLength(50)]
    public string? DepartmentId { get; set; }

    // Department deleted
}
