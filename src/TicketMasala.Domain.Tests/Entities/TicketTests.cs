using FluentAssertions;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Domain.Tests.Entities;

public class TicketTests
{
    [Fact]
    public void Constructor_WithDefaults_HasExpectedInitialValues()
    {
        // Act
        var ticket = new Ticket();

        // Assert
        ticket.DomainId.Should().Be("IT");
        ticket.Status.Should().Be("New");
        ticket.TicketStatus.Should().Be(Status.Pending);
        ticket.EstimatedEffortPoints.Should().Be(0);
        ticket.PriorityScore.Should().Be(0.0);
        ticket.CustomFieldsJson.Should().Be("{}");
        ticket.ReviewStatus.Should().Be(ReviewStatus.None);
    }

    [Fact]
    public void TicketStatus_SetToInProgress_UpdatesStatus()
    {
        // Arrange
        var ticket = new Ticket();

        // Act
        ticket.TicketStatus = Status.InProgress;

        // Assert
        ticket.TicketStatus.Should().Be(Status.InProgress);
    }

    [Fact]
    public void TicketStatus_SetToCompleted_UpdatesStatus()
    {
        // Arrange
        var ticket = new Ticket();

        // Act
        ticket.TicketStatus = Status.Completed;

        // Assert
        ticket.TicketStatus.Should().Be(Status.Completed);
    }

    [Fact]
    public void DomainId_SetToValidValue_PersistsValue()
    {
        // Arrange
        var ticket = new Ticket();

        // Act
        ticket.DomainId = "Support";

        // Assert
        ticket.DomainId.Should().Be("Support");
    }

    [Fact]
    public void Status_SetToValidValue_PersistsValue()
    {
        // Arrange
        var ticket = new Ticket();

        // Act
        ticket.Status = "InProgress";

        // Assert
        ticket.Status.Should().Be("InProgress");
    }

    [Fact]
    public void Title_SetToValidValue_PersistsValue()
    {
        // Arrange
        var ticket = new Ticket();
        var expectedTitle = "Test Ticket Title";

        // Act
        ticket.Title = expectedTitle;

        // Assert
        ticket.Title.Should().Be(expectedTitle);
    }

    [Fact]
    public void Description_SetToValidValue_PersistsValue()
    {
        // Arrange
        var ticket = new Ticket();
        var expectedDescription = "This is a detailed description of the ticket";

        // Act
        ticket.Description = expectedDescription;

        // Assert
        ticket.Description.Should().Be(expectedDescription);
    }

    [Fact]
    public void EstimatedEffortPoints_SetToPositiveValue_PersistsValue()
    {
        // Arrange
        var ticket = new Ticket();

        // Act
        ticket.EstimatedEffortPoints = 5;

        // Assert
        ticket.EstimatedEffortPoints.Should().Be(5);
    }

    [Fact]
    public void PriorityScore_SetToValidValue_PersistsValue()
    {
        // Arrange
        var ticket = new Ticket();

        // Act
        ticket.PriorityScore = 75.5;

        // Assert
        ticket.PriorityScore.Should().Be(75.5);
    }

    [Fact]
    public void CompletionTarget_SetToFutureDate_PersistsValue()
    {
        // Arrange
        var ticket = new Ticket();
        var targetDate = DateTime.UtcNow.AddDays(7);

        // Act
        ticket.CompletionTarget = targetDate;

        // Assert
        ticket.CompletionTarget.Should().Be(targetDate);
    }

    [Fact]
    public void CompletionDate_SetToPastDate_PersistsValue()
    {
        // Arrange
        var ticket = new Ticket();
        var completionDate = DateTime.UtcNow.AddDays(-1);

        // Act
        ticket.CompletionDate = completionDate;

        // Assert
        ticket.CompletionDate.Should().Be(completionDate);
    }

    [Fact]
    public void NavigationProperties_AreInitialized()
    {
        // Arrange & Act
        var ticket = new Ticket();

        // Assert
        ticket.Comments.Should().NotBeNull();
        ticket.SubTickets.Should().NotBeNull();
        ticket.WatcherIds.Should().NotBeNull();
    }

    [Fact]
    public void WatcherIds_AddWatcher_ContainsWatcherId()
    {
        // Arrange
        var ticket = new Ticket();
        var watcherId = "user123";

        // Act
        ticket.WatcherIds.Add(watcherId);

        // Assert
        ticket.WatcherIds.Should().Contain(watcherId);
    }

    [Fact]
    public void GerdaTags_SetToValidValue_PersistsValue()
    {
        // Arrange
        var ticket = new Ticket();
        var tags = "AI-Dispatched,Spam-Cluster";

        // Act
        ticket.GerdaTags = tags;

        // Assert
        ticket.GerdaTags.Should().Be(tags);
    }

    [Fact]
    public void WorkItemTypeCode_SetToValidValue_PersistsValue()
    {
        // Arrange
        var ticket = new Ticket();
        var typeCode = "INCIDENT";

        // Act
        ticket.WorkItemTypeCode = typeCode;

        // Assert
        ticket.WorkItemTypeCode.Should().Be(typeCode);
    }

    [Fact]
    public void AiSummary_SetToValidValue_PersistsValue()
    {
        // Arrange
        var ticket = new Ticket();
        var summary = "AI-generated summary of the ticket";

        // Act
        ticket.AiSummary = summary;

        // Assert
        ticket.AiSummary.Should().Be(summary);
    }
}
