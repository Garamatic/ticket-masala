using FluentAssertions;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Domain.Tests.Entities;

public class TicketCommentTests
{
    [Fact]
    public void Constructor_WithDefaults_InitializesProperties()
    {
        // Act
        var comment = new TicketComment();

        // Assert
        comment.Body.Should().BeEmpty();
        comment.IsInternal.Should().BeFalse();
        comment.TicketId.Should().Be(Guid.Empty);
        comment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Body_SetToValidValue_PersistsValue()
    {
        // Arrange
        var comment = new TicketComment();
        var body = "This is a test comment";

        // Act
        comment.Body = body;

        // Assert
        comment.Body.Should().Be(body);
    }

    [Fact]
    public void IsInternal_SetToTrue_PersistsValue()
    {
        // Arrange
        var comment = new TicketComment();

        // Act
        comment.IsInternal = true;

        // Assert
        comment.IsInternal.Should().BeTrue();
    }

    [Fact]
    public void TicketId_SetToValidValue_PersistsValue()
    {
        // Arrange
        var comment = new TicketComment();
        var ticketId = Guid.NewGuid();

        // Act
        comment.TicketId = ticketId;

        // Assert
        comment.TicketId.Should().Be(ticketId);
    }

    [Fact]
    public void AuthorId_SetToValidValue_PersistsValue()
    {
        // Arrange
        var comment = new TicketComment();
        var authorId = "user123";

        // Act
        comment.AuthorId = authorId;

        // Assert
        comment.AuthorId.Should().Be(authorId);
    }

    [Fact]
    public void CreatedAt_SetToValidValue_PersistsValue()
    {
        // Arrange
        var comment = new TicketComment();
        var createdAt = DateTime.UtcNow;

        // Act
        comment.CreatedAt = createdAt;

        // Assert
        comment.CreatedAt.Should().Be(createdAt);
    }
}
