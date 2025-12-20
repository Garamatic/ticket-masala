using TicketMasala.Web.Models;
using TicketMasala.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Repositories;

/// <summary>
/// EF Core implementation of IUserRepository.
/// </summary>
public class EfCoreUserRepository : IUserRepository
{
    private readonly MasalaDbContext _context;
    private readonly ILogger<EfCoreUserRepository> _logger;

    public EfCoreUserRepository(MasalaDbContext context, ILogger<EfCoreUserRepository> logger)
    {
        _context = context;
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

}
