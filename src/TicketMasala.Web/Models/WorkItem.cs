using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketMasala.Web.Models;

/// <summary>
/// Represents a unit of work in the system.
/// </summary>
public class WorkItem
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [Column(TypeName = "TEXT")]
    public string Payload { get; set; } = "{}";

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public string? Status { get; private set; }
}