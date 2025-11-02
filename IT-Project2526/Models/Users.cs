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
    }
    public class Customer : ApplicationUser
    {
        public string? Code { get; set; }
        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
