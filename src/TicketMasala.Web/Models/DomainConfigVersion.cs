using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketMasala.Web.Models;

/// <summary>
/// Represents a version of the domain configuration.
/// </summary>
public class DomainConfigVersion
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Hash { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "TEXT")]
    public string ConfigurationJson { get; set; } = "{}";
}