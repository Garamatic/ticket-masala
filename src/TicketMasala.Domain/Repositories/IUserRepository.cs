using TicketMasala.Domain.Entities;

namespace TicketMasala.Domain.Repositories;

/// <summary>
/// Repository interface for ApplicationUser and derived types (Customer, Employee).
/// </summary>
public interface IUserRepository
{
    // Employee operations
    Task<Employee?> GetEmployeeByIdAsync(string id);
    Task<IReadOnlyList<Employee>> GetAllEmployeesAsync();
    Task<IReadOnlyList<Employee>> GetEmployeesByTeamAsync(string team);
    Task<IReadOnlyList<Employee>> GetAvailableAgentsAsync();

    // Customer operations
    Task<ApplicationUser?> GetCustomerByIdAsync(string id);
    Task<IReadOnlyList<ApplicationUser>> GetAllCustomersAsync();
    Task<bool> UpdateCustomerAsync(ApplicationUser customer);
    Task<bool> DeleteCustomerAsync(string id);
    Task<bool> CreateCustomerAsync(ApplicationUser customer, string password);

    // General user operations
    Task<ApplicationUser?> GetUserByIdAsync(string id);
    Task<ApplicationUser?> GetUserByEmailAsync(string email);
    Task<IReadOnlyList<ApplicationUser>> GetAllUsersAsync();
    Task<int> CountUsersAsync();
}
