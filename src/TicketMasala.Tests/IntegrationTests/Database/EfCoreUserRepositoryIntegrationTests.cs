using TicketMasala.Tests.TestHelpers;
using TicketMasala.Web.Models;
using Xunit;

namespace TicketMasala.Tests.IntegrationTests.Database;

/// <summary>
/// Integration tests for EfCoreUserRepository using real SQLite database.
/// </summary>
public class EfCoreUserRepositoryIntegrationTests : IDisposable
{
    private readonly DatabaseTestFixture _fixture;

    public EfCoreUserRepositoryIntegrationTests()
    {
        _fixture = new DatabaseTestFixture();
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    #region GetEmployeeByIdAsync Tests

    [Fact]
    public async Task GetEmployeeByIdAsync_WithValidEmployeeId_ReturnsEmployee()
    {
        // Arrange
        var employee = await _fixture.SeedTestEmployeeAsync();

        // Act
        var result = await _fixture.UserRepository.GetEmployeeByIdAsync(employee.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(employee.Id, result.Id);
        Assert.Equal(employee.Email, result.Email);
        Assert.Equal(EmployeeType.Support, result.Level);
    }

    [Fact]
    public async Task GetEmployeeByIdAsync_WithCustomerId_ReturnsNull()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();

        // Act
        var result = await _fixture.UserRepository.GetEmployeeByIdAsync(customer.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetEmployeeByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _fixture.UserRepository.GetEmployeeByIdAsync("invalid-id");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllEmployeesAsync Tests

    [Fact]
    public async Task GetAllEmployeesAsync_ReturnsOnlyEmployees()
    {
        // Arrange
        await _fixture.SeedTestCustomerAsync();
        await _fixture.SeedTestCustomerAsync();
        await _fixture.SeedTestEmployeeAsync();
        await _fixture.SeedTestEmployeeAsync();
        await _fixture.SeedTestEmployeeAsync();

        // Act
        var employees = await _fixture.UserRepository.GetAllEmployeesAsync();

        // Assert
        Assert.Equal(3, employees.Count());
        Assert.All(employees, e => Assert.IsType<Employee>(e));
    }

    [Fact]
    public async Task GetAllEmployeesAsync_WithNoEmployees_ReturnsEmpty()
    {
        // Arrange
        await _fixture.SeedTestCustomerAsync();

        // Act
        var employees = await _fixture.UserRepository.GetAllEmployeesAsync();

        // Assert
        Assert.Empty(employees);
    }

    #endregion

    #region GetEmployeesByTeamAsync Tests

    [Fact]
    public async Task GetEmployeesByTeamAsync_FiltersByTeam()
    {
        // Arrange
        await _fixture.SeedTestEmployeeAsync(team: "Support");
        await _fixture.SeedTestEmployeeAsync(team: "Support");
        await _fixture.SeedTestEmployeeAsync(team: "Development");

        // Act
        var supportTeam = await _fixture.UserRepository.GetEmployeesByTeamAsync("Support");
        var devTeam = await _fixture.UserRepository.GetEmployeesByTeamAsync("Development");

        // Assert
        Assert.Equal(2, supportTeam.Count());
        Assert.Single(devTeam);
        Assert.All(supportTeam, e => Assert.Equal("Support", e.Team));
    }

    [Fact]
    public async Task GetEmployeesByTeamAsync_WithNonExistentTeam_ReturnsEmpty()
    {
        // Arrange
        await _fixture.SeedTestEmployeeAsync(team: "Support");

        // Act
        var result = await _fixture.UserRepository.GetEmployeesByTeamAsync("NonExistent");

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetCustomerByIdAsync Tests

    [Fact]
    public async Task GetCustomerByIdAsync_WithValidCustomerId_ReturnsCustomer()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();

        // Act
        var result = await _fixture.UserRepository.GetCustomerByIdAsync(customer.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(customer.Id, result.Id);
        Assert.Equal(customer.Email, result.Email);
    }

    [Fact]
    public async Task GetCustomerByIdAsync_WithEmployeeId_ReturnsNull()
    {
        // Arrange
        var employee = await _fixture.SeedTestEmployeeAsync();

        // Act
        var result = await _fixture.UserRepository.GetCustomerByIdAsync(employee.Id);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllCustomersAsync Tests

    [Fact]
    public async Task GetAllCustomersAsync_ExcludesEmployees()
    {
        // Arrange
        await _fixture.SeedTestCustomerAsync();
        await _fixture.SeedTestCustomerAsync();
        await _fixture.SeedTestEmployeeAsync();

        // Act
        var customers = await _fixture.UserRepository.GetAllCustomersAsync();

        // Assert
        Assert.Equal(2, customers.Count());
        Assert.All(customers, c => Assert.IsNotType<Employee>(c));
    }

    #endregion

    #region GetUserByIdAsync Tests

    [Fact]
    public async Task GetUserByIdAsync_ReturnsAnyUserType()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync();
        var employee = await _fixture.SeedTestEmployeeAsync();

        // Act
        var customerResult = await _fixture.UserRepository.GetUserByIdAsync(customer.Id);
        var employeeResult = await _fixture.UserRepository.GetUserByIdAsync(employee.Id);

        // Assert
        Assert.NotNull(customerResult);
        Assert.NotNull(employeeResult);
        Assert.Equal(customer.Id, customerResult.Id);
        Assert.Equal(employee.Id, employeeResult.Id);
    }

    #endregion

    #region GetUserByEmailAsync Tests

    [Fact]
    public async Task GetUserByEmailAsync_WithValidEmail_ReturnsUser()
    {
        // Arrange
        var customer = await _fixture.SeedTestCustomerAsync(email: "test@example.com");

        // Act
        var result = await _fixture.UserRepository.GetUserByEmailAsync("test@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(customer.Id, result.Id);
    }

    [Fact]
    public async Task GetUserByEmailAsync_WithInvalidEmail_ReturnsNull()
    {
        // Act
        var result = await _fixture.UserRepository.GetUserByEmailAsync("nonexistent@example.com");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CountUsersAsync Tests

    [Fact]
    public async Task CountUsersAsync_ReturnsCorrectTotalCount()
    {
        // Arrange
        await _fixture.SeedTestCustomerAsync();
        await _fixture.SeedTestCustomerAsync();
        await _fixture.SeedTestEmployeeAsync();

        // Act
        var count = await _fixture.UserRepository.CountUsersAsync();

        // Assert
        Assert.Equal(3, count);
    }

    #endregion

    #region Employee-Specific Field Tests

    [Fact]
    public async Task Employee_GerdaFields_ArePersisted()
    {
        // Arrange
        var employee = new Employee
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "gerda.employee@test.com",
            Email = "gerda.employee@test.com",
            FirstName = "GERDA",
            LastName = "Agent",
            Phone = "555-9999",
            Team = "AI Support",
            Level = EmployeeType.Support,
            Language = "NL,FR",
            Specializations = "[\"Tax Law\",\"Fraud Detection\"]",
            MaxCapacityPoints = 60,
            Region = "Brussels HQ"
        };

        _fixture.Context.Users.Add(employee);
        await _fixture.Context.SaveChangesAsync();

        // Act
        _fixture.Context.ChangeTracker.Clear();
        var result = await _fixture.UserRepository.GetEmployeeByIdAsync(employee.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("NL,FR", result.Language);
        Assert.Equal("[\"Tax Law\",\"Fraud Detection\"]", result.Specializations);
        Assert.Equal(60, result.MaxCapacityPoints);
        Assert.Equal("Brussels HQ", result.Region);
    }

    #endregion
}
