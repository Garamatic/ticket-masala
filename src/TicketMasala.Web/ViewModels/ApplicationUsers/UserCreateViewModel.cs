using System.ComponentModel.DataAnnotations;
using TicketMasala.Web.Utilities;
using TicketMasala.Web.Models;

namespace TicketMasala.Web.ViewModels.ApplicationUsers;

public class UserCreateViewModel
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string FirstName { get; set; }

    [Required]
    public required string LastName { get; set; }

    public string? Phone { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public required string Password { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public required string ConfirmPassword { get; set; }

    [Required]
    public required string Role { get; set; } = "Customer";

    // Employee Fields
    public string? Team { get; set; }
    public EmployeeType? Level { get; set; }
    public string? Language { get; set; }
    public int MaxCapacityPoints { get; set; } = 40;
}
