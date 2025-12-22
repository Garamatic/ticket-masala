using TicketMasala.Domain.Entities;
using TicketMasala.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; // Added for UserManager

namespace TicketMasala.Web.Repositories;

/// <summary>
/// EF Core implementation of IUserRepository.
/// </summary>
public class EfCoreUserRepository : IUserRepository
{
    private readonly MasalaDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager; // Added
    private readonly ILogger<EfCoreUserRepository> _logger;

    public EfCoreUserRepository(
        MasalaDbContext context,
        UserManager<ApplicationUser> userManager, // Added
        ILogger<EfCoreUserRepository> logger)
    {
        _context = context;
        _userManager = userManager; // Added
        _logger = logger;
    }

    public async Task<Employee?> GetEmployeeByIdAsync(string id)
    {
        return await _context.Users.OfType<Employee>()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
    {
        return await _context.Users.OfType<Employee>().ToListAsync();
    }

    public async Task<IEnumerable<Employee>> GetEmployeesByTeamAsync(string team)
    {
        return await _context.Users.OfType<Employee>()
            .Where(e => e.Team == team)
            .ToListAsync();
    }

    public async Task<ApplicationUser?> GetCustomerByIdAsync(string id)
    {
        return await _context.Users
            .Where(u => u.Id == id && !(u is Employee))
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ApplicationUser>> GetAllCustomersAsync()
    {
        return await _context.Users
            .Where(u => !(u is Employee))
            .ToListAsync();
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<int> CountUsersAsync()
    {
        return await _context.Users.CountAsync();
    }

    public async Task<bool> UpdateCustomerAsync(ApplicationUser customer)
    {
        try
        {
            _context.Users.Update(customer);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", customer.Id);
            return false;
        }
    }

    public async Task<bool> DeleteCustomerAsync(string id)
    {
        try
        {
            var customer = await GetCustomerByIdAsync(id);
            if (customer == null)
            {
                return false;
            }

            _context.Users.Remove(customer);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {CustomerId}", id);
            return false;
        }
    }

    public async Task<bool> CreateCustomerAsync(ApplicationUser customer, string password)
    {
        try
        {
            var result = await _userManager.CreateAsync(customer, password);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to create customer {Email}: {Errors}", customer.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }

            // Assign to customer role
            result = await _userManager.AddToRoleAsync(customer, Constants.RoleCustomer);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to add customer {Email} to role {Role}: {Errors}", customer.Email, Constants.RoleCustomer, string.Join(", ", result.Errors.Select(e => e.Description)));
                // Attempt to delete user if role assignment fails to prevent orphaned accounts
                await _userManager.DeleteAsync(customer);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer {Email}", customer.Email);
            return false;
        }
    }
}

