using TicketMasala.Domain.Entities;
using TicketMasala.Web.Engine.GERDA.Dispatching;
using Xunit;

namespace TicketMasala.Tests.Services.GERDA;

public class AffinityScoringTests
{
    [Fact]
    public void CalculateLanguageScore_ExactMatch_Returns5()
    {
        // Arrange
        var agent = new Employee { Language = "en" };
        var customer = new ApplicationUser { Language = "en" };

        // Act
        var score = AffinityScoring.CalculateLanguageScore(agent, customer);

        // Assert
        Assert.Equal(5.0, score);
    }

    [Fact]
    public void CalculateLanguageScore_PartialMatch_Returns4_5()
    {
        // Arrange
        var agent = new Employee { Language = "en,fr" };
        var customer = new ApplicationUser { Language = "en" };

        // Act
        var score = AffinityScoring.CalculateLanguageScore(agent, customer);

        // Assert
        Assert.Equal(4.5, score);
    }

    [Fact]
    public void CalculateGeographyScore_ExactMatch_Returns5()
    {
        // Arrange
        var agent = new Employee { Region = "US" };
        var customer = new ApplicationUser { Region = "US" };

        // Act
        var score = AffinityScoring.CalculateGeographyScore(agent, customer);

        // Assert
        Assert.Equal(5.0, score);
    }

    [Fact]
    public void ExtractCategoryFromTicket_Keywords_ReturnsCorrectCategory()
    {
        // Arrange
        var ticket = new Ticket { Description = "I have a problem with my laptop hardware." };

        // Act
        var category = AffinityScoring.ExtractCategoryFromTicket(ticket);

        // Assert
        Assert.Equal("Hardware Support", category);
    }

    [Fact]
    public void CalculateExpertiseScore_ExactMatch_Returns5()
    {
        // Arrange
        var ticket = new Ticket { Description = "Login failed" }; // Should map to "Password Reset" or similar if keywords match
        // Wait, "Login failed" -> "Password Reset" in ExtractCategoryFromTicket
        
        var agent = new Employee 
        { 
            Specializations = "[\"Password Reset\"]" 
        };

        // Act
        var score = AffinityScoring.CalculateExpertiseScore(ticket, agent);

        // Assert
        Assert.Equal(5.0, score);
    }
}
