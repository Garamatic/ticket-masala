using System;
using System.ComponentModel.DataAnnotations;

namespace IT_Project2526.ViewModels
{
    public class CustomerListViewModel
    {
        private const int MaxNameLength = 100;
        private const int MaxEmailLength = 254;
        private const int MaxProjectCount = 1000000;

        [Required]
        public string Id { get; set; } 

        [Required]
        [StringLength(MaxNameLength)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(MaxNameLength)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(MaxEmailLength)]
        public string Email { get; set; }

        [Range(0, MaxProjectCount)]
        public int ProjectCount { get; set; } // Aantal projecten voor de lijst
    }
}