using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IT_Project2526.ViewModels
{
    public class CustomerListViewModel
    {
        public string Id { get; set; } 
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public int ProjectCount { get; set; } // Aantal projecten voor de lijst
    }
}