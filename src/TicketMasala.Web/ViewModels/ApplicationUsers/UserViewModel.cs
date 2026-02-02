using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Web.ViewModels.ApplicationUsers;

public class UserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Roles { get; set; } = string.Empty; // Comma separated
    public string Type { get; set; } = string.Empty; // Employee or Customer

    public string FullName => $"{FirstName} {LastName}";
}
