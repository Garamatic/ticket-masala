using Microsoft.AspNetCore.Identity;

namespace IT_Project2526.Models
{
    public abstract class ApplicationUser : IdentityUser
    {
        public required string Name { get; set; }
        public required string Phone { get; set; }
    }

    public class Guest : ApplicationUser { }
    public  class Employee : ApplicationUser
    {
        public required string Team { get; set; }
        public required EmployeeLevel Level { get; set; }
    }
    public class Customer : ApplicationUser
    {
        public string? Code { get; set; }
    }
}
