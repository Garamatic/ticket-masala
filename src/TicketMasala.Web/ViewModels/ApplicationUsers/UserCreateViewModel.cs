using System.ComponentModel.DataAnnotations;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Web.ViewModels.ApplicationUsers;

public class UserCreateViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    // Employee Fields
    public string? Team { get; set; }
    public EmployeeType? Level { get; set; }
    public string? Language { get; set; }
    public int MaxCapacityPoints { get; set; } = 40;
}
