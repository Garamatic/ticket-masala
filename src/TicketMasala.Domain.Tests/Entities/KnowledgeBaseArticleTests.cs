using FluentAssertions;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Domain.Tests.Entities;

public class KnowledgeBaseArticleTests
{
    [Fact]
    public void Constructor_WithDefaults_InitializesProperties()
    {
        // Act
        var article = new KnowledgeBaseArticle();

        // Assert
        article.Title.Should().BeEmpty();
        article.Content.Should().BeEmpty();
        article.Tags.Should().BeEmpty();
        article.IsVerified.Should().BeFalse();
        article.UsageCount.Should().Be(0);
    }

    [Fact]
    public void Title_SetToValidValue_PersistsValue()
    {
        // Arrange
        var article = new KnowledgeBaseArticle();
        var title = "How to Reset Password";

        // Act
        article.Title = title;

        // Assert
        article.Title.Should().Be(title);
    }

    [Fact]
    public void Content_SetToValidValue_PersistsValue()
    {
        // Arrange
        var article = new KnowledgeBaseArticle();
        var content = "Step 1: Click forgot password...";

        // Act
        article.Content = content;

        // Assert
        article.Content.Should().Be(content);
    }

    [Fact]
    public void Tags_SetToValidValue_PersistsValue()
    {
        // Arrange
        var article = new KnowledgeBaseArticle();
        var tags = "password,reset,authentication";

        // Act
        article.Tags = tags;

        // Assert
        article.Tags.Should().Be(tags);
    }

    [Fact]
    public void AuthorId_SetToValidValue_PersistsValue()
    {
        // Arrange
        var article = new KnowledgeBaseArticle();
        var authorId = "employee123";

        // Act
        article.AuthorId = authorId;

        // Assert
        article.AuthorId.Should().Be(authorId);
    }

    [Fact]
    public void IsVerified_SetToTrue_PersistsValue()
    {
        // Arrange
        var article = new KnowledgeBaseArticle();

        // Act
        article.IsVerified = true;

        // Assert
        article.IsVerified.Should().BeTrue();
    }

    [Fact]
    public void UsageCount_SetToValidValue_PersistsValue()
    {
        // Arrange
        var article = new KnowledgeBaseArticle();
        var usageCount = 150;

        // Act
        article.UsageCount = usageCount;

        // Assert
        article.UsageCount.Should().Be(usageCount);
    }

    [Fact]
    public void CreatedAt_SetToValidValue_PersistsValue()
    {
        // Arrange
        var article = new KnowledgeBaseArticle();
        var createdAt = DateTime.UtcNow;

        // Act
        article.CreatedAt = createdAt;

        // Assert
        article.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void UpdatedAt_SetToValidValue_PersistsValue()
    {
        // Arrange
        var article = new KnowledgeBaseArticle();
        var updatedAt = DateTime.UtcNow;

        // Act
        article.UpdatedAt = updatedAt;

        // Assert
        article.UpdatedAt.Should().Be(updatedAt);
    }
}
