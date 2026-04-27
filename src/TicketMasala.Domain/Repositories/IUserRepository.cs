using TicketMasala.Domain.Entities;

namespace TicketMasala.Domain.Repositories;

/// <summary>
/// Repository interface for ApplicationUser and derived types (Customer, Employee).
/// </summary>
public interface IUserRepository
{
    // Employee operations
    Task<Employee?> GetEmployeeByIdAsync(string id);
    Task<IEnumerable<Employee>> GetAllEmployeesAsync();
    Task<IEnumerable<Employee>> GetEmployeesByTeamAsync(string team);
    Task<IEnumerable<Employee>> GetAvailableAgentsAsync();

    // Customer operations
    Task<ApplicationUser?> GetCustomerByIdAsync(string id);
    Task<IEnumerable<ApplicationUser>> GetAllCustomersAsync();
    Task<bool> UpdateCustomerAsync(ApplicationUser customer);
    Task<bool> DeleteCustomerAsync(string id);
    Task<bool> CreateCustomerAsync(ApplicationUser customer, string password);

    // General user operations
    Task<ApplicationUser?> GetUserByIdAsync(string id);
    Task<ApplicationUser?> GetUserByEmailAsync(string email);
    Task<IList<ApplicationUser>> GetAllUsersAsync();
    Task<int> CountUsersAsync();
}
