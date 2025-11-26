using IT_Project2526.Models;
using System.ComponentModel.DataAnnotations;

namespace IT_Project2526.ViewModels
{
    public class ProjectViewModel
    {
       [Required]
       public Guid Guid { get; init; }
       [Required, StringLength(200)]
       public string Name { get; init; } = string.Empty;
       [StringLength(2000)]
       public string Description { get; init; } = string.Empty;
       public Status Status { get; init; }
       public Employee ProjectManager {  get; init; }

    }
}
