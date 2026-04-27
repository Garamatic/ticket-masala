using FluentAssertions;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Domain.Tests.Entities;

public class ProjectTests
{
    [Fact]
    public void Constructor_WithDefaults_InitializesCollections()
    {
        // Act
        var project = new Project();

        // Assert
        project.CustomerIds.Should().NotBeNull();
        project.Tasks.Should().NotBeNull();
        project.Customers.Should().NotBeNull();
        project.Resources.Should().NotBeNull();
    }

    [Fact]
    public void Name_SetToValidValue_PersistsValue()
    {
        // Arrange
        var project = new Project();
        var expectedName = "Test Project Name";

        // Act
        project.Name = expectedName;

        // Assert
        project.Name.Should().Be(expectedName);
    }

    [Fact]
    public void Description_SetToValidValue_PersistsValue()
    {
        // Arrange
        var project = new Project();
        var expectedDescription = "This is a detailed project description";

        // Act
        project.Description = expectedDescription;

        // Assert
        project.Description.Should().Be(expectedDescription);
    }

    [Fact]
    public void Status_SetToValidValue_PersistsValue()
    {
        // Arrange
        var project = new Project();

        // Act
        project.Status = Status.InProgress;

        // Assert
        project.Status.Should().Be(Status.InProgress);
    }

    [Fact]
    public void Status_SetToCompleted_PersistsValue()
    {
        // Arrange
        var project = new Project();

        // Act
        project.Status = Status.Completed;

        // Assert
        project.Status.Should().Be(Status.Completed);
    }

    [Fact]
    public void CompletionTarget_SetToFutureDate_PersistsValue()
    {
        // Arrange
        var project = new Project();
        var targetDate = DateTime.UtcNow.AddMonths(3);

        // Act
        project.CompletionTarget = targetDate;

        // Assert
        project.CompletionTarget.Should().Be(targetDate);
    }

    [Fact]
    public void CompletionDate_SetToPastDate_PersistsValue()
    {
        // Arrange
        var project = new Project();
        var completionDate = DateTime.UtcNow.AddDays(-1);

        // Act
        project.CompletionDate = completionDate;

        // Assert
        project.CompletionDate.Should().Be(completionDate);
    }

    [Fact]
    public void CustomerIds_AddCustomer_ContainsCustomerId()
    {
        // Arrange
        var project = new Project();
        var customerId = "customer123";

        // Act
        project.CustomerIds.Add(customerId);

        // Assert
        project.CustomerIds.Should().Contain(customerId);
    }

    [Fact]
    public void ProjectManagerId_SetToValidValue_PersistsValue()
    {
        // Arrange
        var project = new Project();
        var managerId = "manager456";

        // Act
        project.ProjectManagerId = managerId;

        // Assert
        project.ProjectManagerId.Should().Be(managerId);
    }

    [Fact]
    public void CustomerId_SetToValidValue_PersistsValue()
    {
        // Arrange
        var project = new Project();
        var customerId = "customer789";

        // Act
        project.CustomerId = customerId;

        // Assert
        project.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public void ProjectType_SetToValidValue_PersistsValue()
    {
        // Arrange
        var project = new Project();
        var type = "Software Development";

        // Act
        project.ProjectType = type;

        // Assert
        project.ProjectType.Should().Be(type);
    }

    [Fact]
    public void Notes_SetToValidValue_PersistsValue()
    {
        // Arrange
        var project = new Project();
        var notes = "Important project notes and details";

        // Act
        project.Notes = notes;

        // Assert
        project.Notes.Should().Be(notes);
    }

    [Fact]
    public void ProjectAiRoadmap_SetToValidValue_PersistsValue()
    {
        // Arrange
        var project = new Project();
        var roadmap = "AI-generated roadmap for the project";

        // Act
        project.ProjectAiRoadmap = roadmap;

        // Assert
        project.ProjectAiRoadmap.Should().Be(roadmap);
    }

    [Fact]
    public void DepartmentId_SetToValidValue_PersistsValue()
    {
        // Arrange
        var project = new Project();
        var deptId = Guid.NewGuid();

        // Act
        project.DepartmentId = deptId;

        // Assert
        project.DepartmentId.Should().Be(deptId);
    }
}
