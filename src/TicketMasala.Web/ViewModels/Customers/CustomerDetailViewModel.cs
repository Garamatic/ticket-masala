using System;
using System.Collections.Generic;
using System.Linq;

namespace TicketMasala.Web.ViewModels.Customers;

using TicketMasala.Web.ViewModels.Projects;

public class CustomerDetailViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    // Add FirstName and LastName for backward compatibility
    public string FirstName => Name.Split(' ').FirstOrDefault() ?? string.Empty;
    public string LastName => Name.Split(' ').Skip(1).FirstOrDefault() ?? string.Empty;

    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public IReadOnlyList<ProjectViewModel> Projects { get; init; } = Array.Empty<ProjectViewModel>();
}
