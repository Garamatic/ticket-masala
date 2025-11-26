using System;
using System.Collections.Generic;

namespace IT_Project2526.ViewModels
{
    public class CustomerDetailViewModel
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Phone { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public IReadOnlyList<ProjectViewModel> Projects { get; init; } = Array.Empty<ProjectViewModel>();
    }
}
