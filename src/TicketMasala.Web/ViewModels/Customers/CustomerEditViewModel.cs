using System.ComponentModel.DataAnnotations;
using TicketMasala.Web.Utilities;

namespace TicketMasala.Web.ViewModels.Customers;

public class CustomerEditViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required")]
    [NoHtml(ErrorMessage = "First name cannot contain HTML")]
    [SafeStringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [NoHtml(ErrorMessage = "Last name cannot contain HTML")]
    [SafeStringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [SafeStringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number format")]
    [SafeStringLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }
}
