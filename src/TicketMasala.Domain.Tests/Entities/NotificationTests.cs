using FluentAssertions;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Domain.Tests.Entities;

public class NotificationTests
{
    [Fact]
    public void Constructor_WithDefaults_InitializesProperties()
    {
        // Act
        var notification = new Notification();

        // Assert
        notification.UserId.Should().BeEmpty();
        notification.Message.Should().BeEmpty();
        notification.Type.Should().Be("Info");
        notification.IsRead.Should().BeFalse();
        notification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UserId_SetToValidValue_PersistsValue()
    {
        // Arrange
        var notification = new Notification();
        var userId = "user123";

        // Act
        notification.UserId = userId;

        // Assert
        notification.UserId.Should().Be(userId);
    }

    [Fact]
    public void Message_SetToValidValue_PersistsValue()
    {
        // Arrange
        var notification = new Notification();
        var message = "You have been assigned to ticket #1234";

        // Act
        notification.Message = message;

        // Assert
        notification.Message.Should().Be(message);
    }

    [Fact]
    public void Type_SetToValidValue_PersistsValue()
    {
        // Arrange
        var notification = new Notification();
        var type = "Warning";

        // Act
        notification.Type = type;

        // Assert
        notification.Type.Should().Be(type);
    }

    [Fact]
    public void LinkUrl_SetToValidValue_PersistsValue()
    {
        // Arrange
        var notification = new Notification();
        var linkUrl = "/ticket/1234";

        // Act
        notification.LinkUrl = linkUrl;

        // Assert
        notification.LinkUrl.Should().Be(linkUrl);
    }

    [Fact]
    public void IsRead_SetToTrue_PersistsValue()
    {
        // Arrange
        var notification = new Notification();

        // Act
        notification.IsRead = true;

        // Assert
        notification.IsRead.Should().BeTrue();
    }

    [Fact]
    public void CreatedAt_SetToValidValue_PersistsValue()
    {
        // Arrange
        var notification = new Notification();
        var createdAt = DateTime.UtcNow;

        // Act
        notification.CreatedAt = createdAt;

        // Assert
        notification.CreatedAt.Should().Be(createdAt);
    }
}
