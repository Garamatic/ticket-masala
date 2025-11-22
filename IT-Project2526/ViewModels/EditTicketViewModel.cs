using IT_Project2526.Models;
using Microsoft.AspNetCore.Mvc.Rendering; 
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IT_Project2526.ViewModels
{
    public class EditTicketViewModel
    {
        public Guid Guid { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [Display(Name = "Current Status")]
        public Status TicketStatus { get; set; }

        [Display(Name = "Target Completion Date")]
        [DataType(DataType.Date)]
        public DateTime? CompletionTarget { get; set; }

        // Selectielijst voor het kiezen van een verantwoordelijke
        [Display(Name = "Responsible")]
        public string? ResponsibleUserId { get; set; }
        public List<SelectListItem> ResponsibleUsers { get; set; } = new List<SelectListItem>();
    }
}
