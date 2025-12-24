using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace TicketMasala.Web.Data.Seeding;

/// <summary>
/// Seed strategy for creating users and employees from configuration.
/// </summary>
public class UserSeedStrategy : ISeedStrategy
{
    private readonly MasalaDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UserSeedStrategy> _logger;

    public UserSeedStrategy(
        MasalaDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment,
        ILogger<UserSeedStrategy> logger)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
        _logger = logger;
    }

    public async Task<bool> ShouldSeedAsync()
    {
        // Seed if no users exist yet
        var userCount = await _context.Users.CountAsync();
        return userCount == 0;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Seeding users and employees...");

        var config = await LoadSeedConfigurationAsync();
        if (config == null)
        {
            _logger.LogWarning("No seed configuration found, skipping user seeding");
            return;
        }

        // Seed Admins
        if (config.Admins?.Count > 0)
        {
            await CreateUsersAsync(config.Admins, Constants.RoleAdmin, "Admin123!");
        }

        // Seed Employees
        if (config.Employees?.Count > 0)
        {
            await CreateEmployeesAsync(config.Employees, "Employee123!");
        }

        // Seed Customers
        if (config.Customers?.Count > 0)
        {
            await CreateUsersAsync(config.Customers, Constants.RoleCustomer, "Customer123!");
        }

        _logger.LogInformation("Users and employees seeded successfully");
    }

    private async Task<SeedConfig?> LoadSeedConfigurationAsync()
    {
        var seedFilePath = TicketMasala.Web.Configuration.ConfigurationPaths.GetConfigFilePath(
            _environment.ContentRootPath,
            "seed_data.json");

        if (!File.Exists(seedFilePath))
        {
            _logger.LogWarning("Seed data file not found at: {Path}", seedFilePath);
            return null;
        }

        try
        {
            _logger.LogInformation("Loading seed data from: {Path}", seedFilePath);
            var json = await File.ReadAllTextAsync(seedFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            return JsonSerializer.Deserialize<SeedConfig>(json, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing seed data JSON from {Path}", seedFilePath);
            return null;
        }
    }

    private async Task CreateUsersAsync(List<SeedUser> users, string role, string defaultPassword)
    {
        if (users == null || users.Count == 0)
        {
            _logger.LogWarning("No users provided for role {Role}. Skipping.", role);
            return;
        }

        foreach (var userDto in users)
        {
            var user = new ApplicationUser
            {
                UserName = userDto.UserName,
                Email = userDto.Email,
                EmailConfirmed = true,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Phone = userDto.Phone,
                Code = userDto.Code
            };

            var result = await _userManager.CreateAsync(user, defaultPassword);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                _logger.LogInformation("Created {Role} user: {Email}", role, user.Email);
            }
            else
            {
                _logger.LogError("Failed to create {Role} user {Email}: {Errors}",
                    role, user.Email, string.Join(", ", result.Errors?.Select(e => e.Description) ?? new string[0]));
            }
        }
    }

    private async Task CreateEmployeesAsync(List<SeedUser> employees, string defaultPassword)
    {
        if (employees == null || employees.Count == 0)
        {
            _logger.LogWarning("No employees provided. Skipping.");
            return;
        }

        foreach (var empDto in employees)
        {
            var employee = new Employee
            {
                UserName = empDto.UserName,
                Email = empDto.Email,
                EmailConfirmed = true,
                FirstName = empDto.FirstName,
                LastName = empDto.LastName,
                Phone = empDto.Phone,
                Team = empDto.Team ?? string.Empty,
                Level = empDto.Level ?? EmployeeType.Support,
                // GERDA Fields
                Language = empDto.Language,
                Specializations = empDto.Specializations,
                MaxCapacityPoints = empDto.MaxCapacityPoints ?? 0,
                Region = empDto.Region
            };

            var result = await _userManager.CreateAsync(employee, defaultPassword);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(employee, Constants.RoleEmployee);
                _logger.LogInformation("Created employee user: {Email}", employee.Email);
            }
            else
            {
                _logger.LogError("Failed to create employee user {Email}: {Errors}",
                    employee.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
