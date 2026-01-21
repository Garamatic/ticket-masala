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
    private readonly IReadOnlyDictionary<string, EmployeeType> _employeeTypeAliases;

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
        _employeeTypeAliases = LoadEmployeeTypeAliases();
    }

    public async Task<bool> ShouldSeedAsync()
    {
        Console.WriteLine("DEBUG: UserSeedStrategy.ShouldSeedAsync called");
        return true;
    }

    public async Task SeedAsync()
    {
        Console.WriteLine("DEBUG: UserSeedStrategy.SeedAsync called!");
        _logger.LogInformation("Seeding users and employees...");

        var adminPassword = GetSeedPassword("MASALA_SEEDED_ADMIN_PASSWORD", "Admin123!");
        var employeePassword = GetSeedPassword("MASALA_SEEDED_EMPLOYEE_PASSWORD", "Employee123!");
        var customerPassword = GetSeedPassword("MASALA_SEEDED_CUSTOMER_PASSWORD", "Customer123!");

        var config = await LoadSeedConfigurationAsync();
        if (config == null)
        {
            _logger.LogWarning("No seed configuration found, skipping user seeding");
            return;
        }

        // Seed Admins
        if (config.Admins?.Count > 0)
        {
            await CreateOrUpdateUsersAsync(config.Admins, Constants.RoleAdmin, adminPassword);
        }

        // Seed Employees
        if (config.Employees?.Count > 0)
        {
            await CreateEmployeesAsync(config.Employees, employeePassword);
        }

        // Seed Customers
        if (config.Customers?.Count > 0)
        {
            await CreateOrUpdateUsersAsync(config.Customers, Constants.RoleCustomer, customerPassword);
        }

        _logger.LogInformation("Users and employees seeded successfully");
    }

    private async Task<SeedConfig?> LoadSeedConfigurationAsync()
    {
        var seedFilePath = TicketMasala.Web.Configuration.ConfigurationPaths.GetConfigFilePath(
            _environment.ContentRootPath,
            "seed_data.json");
        
        Console.WriteLine($"Attempting to load seed data from: {seedFilePath}");
        _logger.LogInformation("Attempting to load seed data from: {Path}", seedFilePath);

        if (!File.Exists(seedFilePath))
        {
            Console.WriteLine($"Seed data file NOT FOUND at: {seedFilePath}");
            _logger.LogWarning("Seed data file not found at: {Path}", seedFilePath);

            // List files in directory
            var directory = Path.GetDirectoryName(seedFilePath);
            if (Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory);
                Console.WriteLine($"Files in {directory}: {string.Join(", ", files.Select(Path.GetFileName))}");
            }
            else 
            {
                Console.WriteLine($"Directory NOT FOUND: {directory}");
            }

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

    private async Task CreateOrUpdateUsersAsync(List<SeedUser> users, string role, string defaultPassword)
    {
        if (users == null || users.Count == 0)
        {
            _logger.LogWarning("No users provided for role {Role}. Skipping.", role);
            return;
        }

        foreach (var userDto in users)
        {
            var user = await _userManager.FindByNameAsync(userDto.UserName);
            if (user == null)
            {
                // Create new
                user = new ApplicationUser
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
            else
            {
                // Update existing - Reset Password
                _logger.LogInformation("User {UserName} exists. Resetting password...", user.UserName);

                await ResetPasswordWithRetryAsync(user, defaultPassword, user.UserName ?? user.Email ?? user.Id);

                // Ensure role
                if (!await _userManager.IsInRoleAsync(user, role))
                {
                    await _userManager.AddToRoleAsync(user, role);
                }
            }
        }
    }

    private string GetSeedPassword(string envVarName, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(envVarName);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (_environment.IsProduction())
        {
            _logger.LogWarning("{EnvVar} is not set; using insecure fallback password in Production.", envVarName);
        }

        return fallback;
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
            var existingUser = await _userManager.FindByNameAsync(empDto.UserName);
            if (existingUser == null)
            {
                // Create new employee
                var employee = new Employee
                {
                    UserName = empDto.UserName,
                    Email = empDto.Email,
                    EmailConfirmed = true,
                    FirstName = empDto.FirstName,
                    LastName = empDto.LastName,
                    Phone = empDto.Phone,
                    Team = empDto.Team ?? string.Empty,
                    Level = ResolveEmployeeType(empDto.Level),
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
            else
            {
                // Update existing user - Reset Password
                _logger.LogInformation("Employee {UserName} exists. Resetting password...", existingUser.UserName);

                await ResetPasswordWithRetryAsync(existingUser, defaultPassword, existingUser.UserName ?? existingUser.Email ?? existingUser.Id, isEmployee: true);

                // Ensure role
                if (!await _userManager.IsInRoleAsync(existingUser, Constants.RoleEmployee))
                {
                    await _userManager.AddToRoleAsync(existingUser, Constants.RoleEmployee);
                }

                // If it's not already an Employee, we can't update the Employee-specific fields
                // But password reset should be sufficient
                if (existingUser is Employee employee)
                {
                    // Update other fields if needed
                    employee.FirstName = empDto.FirstName;
                    employee.LastName = empDto.LastName;
                    employee.Phone = empDto.Phone;
                    employee.Team = empDto.Team ?? string.Empty;
                    employee.Level = ResolveEmployeeType(empDto.Level);
                    employee.Language = empDto.Language;
                    employee.Specializations = empDto.Specializations;
                    employee.MaxCapacityPoints = empDto.MaxCapacityPoints ?? 0;
                    employee.Region = empDto.Region;

                    await _userManager.UpdateAsync(employee);
                }
            }
        }
    }

    private async Task ResetPasswordWithRetryAsync(
        ApplicationUser user,
        string newPassword,
        string userNameForLogging,
        bool isEmployee = false,
        int maxAttempts = 2)
    {
        var current = user;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(current);
            var result = await _userManager.ResetPasswordAsync(current, token, newPassword);

            if (result.Succeeded)
            {
                if (isEmployee)
                {
                    _logger.LogInformation("Password reset for employee {UserName} to default.", userNameForLogging);
                }
                else
                {
                    _logger.LogInformation("Password reset for {UserName} to default.", userNameForLogging);
                }

                return;
            }

            if (IsConcurrencyFailure(result) && attempt < maxAttempts)
            {
                _logger.LogWarning(
                    "Optimistic concurrency while resetting password for {UserName} (attempt {Attempt}/{Max}). Retrying...",
                    userNameForLogging,
                    attempt,
                    maxAttempts);

                var reloaded = await _userManager.FindByIdAsync(current.Id);
                if (reloaded != null)
                {
                    current = reloaded;
                }

                continue;
            }

            var errors = result.Errors?.Select(e => e.Description) ?? Array.Empty<string>();
            if (isEmployee)
            {
                _logger.LogError("Failed to reset password for employee {UserName}: {Errors}", userNameForLogging, string.Join(", ", errors));
            }
            else
            {
                _logger.LogError("Failed to reset password for {UserName}: {Errors}", userNameForLogging, string.Join(", ", errors));
            }

            return;
        }
    }

    private static bool IsConcurrencyFailure(IdentityResult result)
    {
        return result.Errors?.Any(e => string.Equals(e.Code, "ConcurrencyFailure", StringComparison.OrdinalIgnoreCase)) == true;
    }

    private EmployeeType ResolveEmployeeType(string? rawLevel)
    {
        if (string.IsNullOrWhiteSpace(rawLevel))
        {
            return EmployeeType.Support;
        }

        var normalized = rawLevel.Trim();

        if (Enum.TryParse<EmployeeType>(normalized, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        if (_employeeTypeAliases.TryGetValue(normalized, out var mapped))
        {
            return mapped;
        }

        return EmployeeType.Support;
    }

    private IReadOnlyDictionary<string, EmployeeType> LoadEmployeeTypeAliases()
    {
        var aliases = new Dictionary<string, EmployeeType>(StringComparer.OrdinalIgnoreCase);

        var configPath = TicketMasala.Web.Configuration.ConfigurationPaths.GetConfigFilePath(
            _environment.ContentRootPath,
            "masala_config.json");

        if (!File.Exists(configPath))
        {
            return aliases;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
            var root = doc.RootElement;

            if (!TryGetPropertyIgnoreCase(root, "EmployeeTypeAliases", out var mapElement))
            {
                if (TryGetPropertyIgnoreCase(root, "Employees", out var employeesElement) &&
                    employeesElement.ValueKind == JsonValueKind.Object &&
                    !TryGetPropertyIgnoreCase(employeesElement, "EmployeeTypeAliases", out mapElement))
                {
                    return aliases;
                }
            }

            if (mapElement.ValueKind != JsonValueKind.Object)
            {
                return aliases;
            }

            foreach (var kvp in mapElement.EnumerateObject())
            {
                var targetRaw = kvp.Value.GetString();
                if (string.IsNullOrWhiteSpace(targetRaw))
                {
                    continue;
                }

                if (Enum.TryParse<EmployeeType>(targetRaw.Trim(), ignoreCase: true, out var target))
                {
                    aliases[kvp.Name] = target;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse employee type aliases from {Path}", configPath);
        }

        return aliases;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
