using System.ComponentModel.DataAnnotations;
using TicketMasala.Web.Utilities;
using TicketMasala.Web.Models;

namespace TicketMasala.Web.ViewModels.ApplicationUsers;

public class UserEditViewModel
{
    public required string Id { get; set; }

    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    public string? UserName { get; set; }

    [Required]
    public required string FirstName { get; set; }

    [Required]
    public required string LastName { get; set; }

    public string? Phone { get; set; }

    [Required]
    public required string Role { get; set; }

    // Employee Fields
    public string? Team { get; set; }
    public EmployeeType? Level { get; set; }
    public string? Language { get; set; }
    public int MaxCapacityPoints { get; set; }

    // Logic to determine if fields are visible
    public bool IsEmployee => Role == "Employee" || Role == "Admin";
}
