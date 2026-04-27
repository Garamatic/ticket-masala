using FluentAssertions;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Domain.Tests.Entities;

public class EmployeeTests
{
    [Fact]
    public void Constructor_WithDefaults_InitializesEmployeeTypeAndTeam()
    {
        // Act
        var employee = new Employee();

        // Assert
        employee.Team.Should().BeEmpty();
        employee.MaxCapacityPoints.Should().Be(40);
    }

    [Fact]
    public void Team_SetToValidValue_PersistsValue()
    {
        // Arrange
        var employee = new Employee();
        var team = "Engineering";

        // Act
        employee.Team = team;

        // Assert
        employee.Team.Should().Be(team);
    }

    [Fact]
    public void Level_SetToValidValue_PersistsValue()
    {
        // Arrange
        var employee = new Employee();

        // Act
        employee.Level = EmployeeType.Developer;

        // Assert
        employee.Level.Should().Be(EmployeeType.Developer);
    }

    [Fact]
    public void Level_SetToManager_PersistsValue()
    {
        // Arrange
        var employee = new Employee();

        // Act
        employee.Level = EmployeeType.ProjectManager;

        // Assert
        employee.Level.Should().Be(EmployeeType.ProjectManager);
    }

    [Fact]
    public void MaxCapacityPoints_SetToValidValue_PersistsValue()
    {
        // Arrange
        var employee = new Employee();
        var capacity = 50;

        // Act
        employee.MaxCapacityPoints = capacity;

        // Assert
        employee.MaxCapacityPoints.Should().Be(capacity);
    }

    [Fact]
    public void Specializations_SetToValidValue_PersistsValue()
    {
        // Arrange
        var employee = new Employee();
        var specializations = "[\"Tax Law\", \"Fraud Detection\"]";

        // Act
        employee.Specializations = specializations;

        // Assert
        employee.Specializations.Should().Be(specializations);
    }

    [Fact]
    public void ProfilePicturePath_SetToValidValue_PersistsValue()
    {
        // Arrange
        var employee = new Employee();
        var path = "/images/employees/profile.jpg";

        // Act
        employee.ProfilePicturePath = path;

        // Assert
        employee.ProfilePicturePath.Should().Be(path);
    }

    [Fact]
    public void DepartmentId_SetToValidValue_PersistsValue()
    {
        // Arrange
        var employee = new Employee();
        var deptId = "DEPT001";

        // Act
        employee.DepartmentId = deptId;

        // Assert
        employee.DepartmentId.Should().Be(deptId);
    }

    [Fact]
    public void InheritsFromApplicationUser()
    {
        // Arrange & Act
        var employee = new Employee
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@example.com",
            Team = "IT Support"
        };

        // Assert
        employee.Name.Should().Be("Jane Smith");
        employee.Email.Should().Be("jane@example.com");
        employee.Team.Should().Be("IT Support");
    }
}
