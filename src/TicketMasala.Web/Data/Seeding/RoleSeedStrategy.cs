using Microsoft.AspNetCore.Identity;

namespace TicketMasala.Web.Data.Seeding;

/// <summary>
/// Seed strategy for creating default roles (Admin, Employee, Customer).
/// Executes first to ensure role infrastructure exists for user seeding.
/// </summary>
public class RoleSeedStrategy : ISeedStrategy
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<RoleSeedStrategy> _logger;

    public RoleSeedStrategy(
        RoleManager<IdentityRole> roleManager,
        ILogger<RoleSeedStrategy> logger)
    {
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<bool> ShouldSeedAsync()
    {
        // Seed if any of the default roles are missing
        return !(await _roleManager.RoleExistsAsync("Admin") &&
                 await _roleManager.RoleExistsAsync("Employee") &&
                 await _roleManager.RoleExistsAsync("Customer"));
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Seeding roles...");

        await EnsureRoleExistsAsync("Admin");
        await EnsureRoleExistsAsync("Employee");
        await EnsureRoleExistsAsync("Customer");

        _logger.LogInformation("Roles seeded successfully");
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            _logger.LogInformation("Creating role: {RoleName}", roleName);
            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create role {RoleName}: {Errors}", roleName, errors);
                throw new InvalidOperationException($"Failed to create role {roleName}: {errors}");
            }
        }
        else
        {
            _logger.LogDebug("Role already exists: {RoleName}", roleName);
        }
    }
}
