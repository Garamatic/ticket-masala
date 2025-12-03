using IT_Project2526.Models;
using Microsoft.EntityFrameworkCore;

namespace IT_Project2526.Repositories;

/// <summary>
/// EF Core implementation of IUserRepository.
/// </summary>
public class EfCoreUserRepository : IUserRepository
{
    private readonly ITProjectDB _context;
    private readonly ILogger<EfCoreUserRepository> _logger;

    public EfCoreUserRepository(ITProjectDB context, ILogger<EfCoreUserRepository> logger)
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

    public async Task<Customer?> GetCustomerByIdAsync(string id)
    {
        return await _context.Users.OfType<Customer>()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
    {
        return await _context.Users.OfType<Customer>().ToListAsync();
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
}
