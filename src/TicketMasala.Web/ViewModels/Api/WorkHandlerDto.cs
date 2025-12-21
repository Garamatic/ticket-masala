using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Web.ViewModels.Api;

/// <summary>
/// Domain-agnostic DTO for WorkHandler (maps to ApplicationUser/Employee entity).
/// Provides a consistent API interface for user/handler management.
/// </summary>
public class WorkHandlerDto
{
    /// <summary>
    /// Unique identifier for the work handler
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the work handler
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the work handler
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Phone number of the work handler
    /// </summary>
    [StringLength(50)]
    public string? Phone { get; set; }

    /// <summary>
    /// Username for the work handler
    /// </summary>
    [StringLength(256)]
    public string? UserName { get; set; }

    /// <summary>
    /// Team or department the work handler belongs to
    /// </summary>
    [StringLength(100)]
    public string? Team { get; set; }

    /// <summary>
    /// Role or level of the work handler (e.g., "Admin", "Support", "Developer")
    /// </summary>
    public string? Level { get; set; }

    /// <summary>
    /// Primary language(s) spoken by the handler (e.g., "NL", "FR", "EN")
    /// </summary>
    [StringLength(50)]
    public string? Language { get; set; }

    /// <summary>
    /// Areas of specialization or expertise
    /// </summary>
    public List<string> Specializations { get; set; } = new List<string>();

    /// <summary>
    /// Maximum effort points this handler can handle concurrently
    /// </summary>
    public int MaxCapacityPoints { get; set; }

    /// <summary>
    /// Geographic region or office location
    /// </summary>
    [StringLength(100)]
    public string? Region { get; set; }

    /// <summary>
    /// Whether the handler is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Path to the handler's profile picture
    /// </summary>
    public string? ProfilePicturePath { get; set; }
}