using IT_Project2526.Models;

namespace IT_Project2526.Repositories;

/// <summary>
/// Repository interface for ApplicationUser and derived types (Customer, Employee).
/// </summary>
public interface IUserRepository
{
    // Employee operations
    Task<Employee?> GetEmployeeByIdAsync(string id);
    Task<IEnumerable<Employee>> GetAllEmployeesAsync();
    Task<IEnumerable<Employee>> GetEmployeesByTeamAsync(string team);
    
    // Customer operations
    Task<Customer?> GetCustomerByIdAsync(string id);
    Task<IEnumerable<Customer>> GetAllCustomersAsync();
    
    // General user operations
    Task<ApplicationUser?> GetUserByIdAsync(string id);
    Task<ApplicationUser?> GetUserByEmailAsync(string email);
    Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();
    Task<int> CountUsersAsync();
}
