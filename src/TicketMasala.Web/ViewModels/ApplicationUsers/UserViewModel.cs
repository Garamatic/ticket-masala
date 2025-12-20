using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Web.ViewModels.ApplicationUsers;

public class UserViewModel
{
    public required string Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public required string Roles { get; set; } // Comma separated
    public required string Type { get; set; } // Employee or Customer
    
    public string FullName => $"{FirstName} {LastName}";
}
