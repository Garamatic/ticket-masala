using System.ComponentModel.DataAnnotations;

namespace IT_Project2526.Models
{
    public class Department
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty; // e.g., "FIS", "HR"

        public List<Project> Projects { get; set; } = new();
        public List<Employee> Employees { get; set; } = new();
    }
}
