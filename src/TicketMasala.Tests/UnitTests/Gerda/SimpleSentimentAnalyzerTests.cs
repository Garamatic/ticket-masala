using TicketMasala.Web.Engine.GERDA.Sentiment;
using Xunit;

namespace TicketMasala.Tests.UnitTests.Gerda;

public class SimpleSentimentAnalyzerTests
{
    [Fact]
    public void Analyze_UrgentKeywords_ReturnsCriticalScore()
    {
        // Arrange
        var subject = "System Down";
        var body = "This is a CRITICAL emergency!";

        // Act
        var (score, label) = SimpleSentimentAnalyzer.Analyze(subject, body);

        // Assert
        Assert.True(score >= 4.0, $"Expected score >= 4.0 but got {score}");
        Assert.Equal("Critical", label);
    }

    [Fact]
    public void Analyze_NeutralText_ReturnsNormalScore()
    {
        // Arrange
        var subject = "Coffee Request";
        var body = "I would like some beans please.";

        // Act
        var (score, label) = SimpleSentimentAnalyzer.Analyze(subject, body);

        // Assert
        Assert.Equal(1.0, score);
        Assert.Equal("Normal", label);
    }

    [Fact]
    public void Analyze_NegativeText_ReturnsBoostedScore()
    {
        // Arrange
        var subject = "Bug Report";
        var body = "The app crashed and I am disappointed.";

        // Act
        var (score, label) = SimpleSentimentAnalyzer.Analyze(subject, body);

        // Assert
        // Base 1.0 + Negative (0.5 for keyword) -> Expect > 1.0
        Assert.True(score > 1.0, $"Expected score > 1.0 but got {score}");
    }
}
