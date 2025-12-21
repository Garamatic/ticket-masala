using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using TicketMasala.Domain.Common;

namespace TicketMasala.Web.ViewModels.Projects;

public class NewProject
{
    [Display(Name = "Project")]
    public Guid Guid { get; set; }

    // Projectvelden
    [Required(ErrorMessage = "Project name is required")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Project name must be between 3 and 200 characters")]
    [Display(Name = "Project Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 2000 characters")]
    [Display(Name = "Project Description")]
    public string Description { get; set; } = string.Empty;

    // Optional creation date and type for binding from the view
    [Display(Name = "Creation Date")]
    [DataType(DataType.Date)]
    public DateTime? CreationDate { get; set; }

    [Required(ErrorMessage = "Please select a project type")]
    [Display(Name = "Project Type")]
    public string? ProjectType { get; set; }

    [StringLength(1000, ErrorMessage = "Comments cannot exceed 1000 characters")]
    [Display(Name = "Comments")]
    public string? ProjectComment { get; set; }

    // Klantenkeuze
    [Display(Name = "Customer Type")]
    public bool IsNewCustomer { get; set; } = true; // Standaard staat 'Nieuwe klant' geselecteerd

    [RequiredIf(nameof(IsNewCustomer), false, ErrorMessage = "Please select an existing customer")]
    public string? SelectedCustomerId { get; set; }

    public List<SelectListItem> CustomerList { get; set; } = new List<SelectListItem>();

    // Velden voor een nieuwe klant
    [RequiredIf(nameof(IsNewCustomer), true, ErrorMessage = "First name is required for new customer")]
    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
    [ValidName(ErrorMessage = "First name contains invalid characters")]
    [Display(Name = "First Name")]
    public string? NewCustomerFirstName { get; set; }

    [RequiredIf(nameof(IsNewCustomer), true, ErrorMessage = "Last name is required for new customer")]
    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
    [ValidName(ErrorMessage = "Last name contains invalid characters")]
    [Display(Name = "Last Name")]
    public string? NewCustomerLastName { get; set; }

    [RequiredIf(nameof(IsNewCustomer), true, ErrorMessage = "Email is required for new customer")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [Display(Name = "Email Address")]
    public string? NewCustomerEmail { get; set; }

    [Phone(ErrorMessage = "Invalid phone number")]
    [Display(Name = "Phone Number")]
    public string? NewCustomerPhone { get; set; }
    [Display(Name = "Project Template")]
    public Guid? SelectedTemplateId { get; set; }
    public List<SelectListItem> TemplateList { get; set; } = new List<SelectListItem>();

    [Display(Name = "Additional Stakeholders")]
    public List<string> SelectedStakeholderIds { get; set; } = new List<string>();
    public List<SelectListItem> StakeholderList { get; set; } = new List<SelectListItem>();

    [Display(Name = "Project Manager")]
    public string? SelectedProjectManagerId { get; set; }
    public List<SelectListItem> ProjectManagerList { get; set; } = new List<SelectListItem>();
}
