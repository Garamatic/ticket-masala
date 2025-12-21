using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities; // ApplicationUser, Employee
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Web.ViewModels.Tickets;

public class EditTicketViewModel
{
    public Guid Guid { get; set; }

    [Required(ErrorMessage = "Description is required")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Current Status")]
    public Status TicketStatus { get; set; }

    [Display(Name = "Target Completion Date")]
    [DataType(DataType.Date)]
    public DateTime? CompletionTarget { get; set; }

    // Customer Selection
    [Display(Name = "Customer")]
    public string? CustomerId { get; set; }
    public List<SelectListItem> CustomerList { get; set; } = new List<SelectListItem>();

    // Project Selection
    [Display(Name = "Project")]
    public Guid? ProjectGuid { get; set; }
    public List<SelectListItem> ProjectList { get; set; } = new List<SelectListItem>();

    // Selectielijst voor het kiezen van een verantwoordelijke
    [Display(Name = "Responsible")]
    public string? ResponsibleUserId { get; set; }
    public List<SelectListItem> ResponsibleUsers { get; set; } = new List<SelectListItem>();
}
