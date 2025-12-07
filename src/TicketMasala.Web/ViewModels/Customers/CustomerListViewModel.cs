using System;
using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Web.ViewModels.Customers;

public class CustomerListViewModel
{
        private const int MaxNameLength = 100;
        private const int MaxEmailLength = 254;
        private const int MaxProjectCount = 1000000;

        [Required]
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(MaxNameLength)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(MaxNameLength)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(MaxEmailLength)]
        public string Email { get; set; } = string.Empty;

        [Range(0, MaxProjectCount)]
        public int ProjectCount { get; set; } // Aantal projecten voor de lijst
}
