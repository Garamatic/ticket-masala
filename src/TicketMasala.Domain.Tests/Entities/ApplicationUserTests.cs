using FluentAssertions;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Domain.Tests.Entities;

public class ApplicationUserTests
{
    [Fact]
    public void Constructor_WithDefaults_SetsEmptyStrings()
    {
        // Act
        var user = new ApplicationUser();

        // Assert
        user.FirstName.Should().BeEmpty();
        user.LastName.Should().BeEmpty();
        user.UserName.Should().BeNull();
        user.Email.Should().BeNull();
    }

    [Fact]
    public void FirstName_SetToValidValue_PersistsValue()
    {
        // Arrange
        var user = new ApplicationUser();
        var firstName = "John";

        // Act
        user.FirstName = firstName;

        // Assert
        user.FirstName.Should().Be(firstName);
    }

    [Fact]
    public void LastName_SetToValidValue_PersistsValue()
    {
        // Arrange
        var user = new ApplicationUser();
        var lastName = "Doe";

        // Act
        user.LastName = lastName;

        // Assert
        user.LastName.Should().Be(lastName);
    }

    [Fact]
    public void Name_ReturnsFirstAndLastNameCombined()
    {
        // Arrange
        var user = new ApplicationUser
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Act & Assert
        user.Name.Should().Be("John Doe");
    }

    [Fact]
    public void Name_TrimsFinalResult()
    {
        // Arrange
        var user = new ApplicationUser
        {
            FirstName = "  John  ",
            LastName = "  "
        };

        // Act & Assert - only the final result is trimmed, not individual parts
        user.Name.Should().Be("John");
    }

    [Fact]
    public void Name_WhenFirstNameEmpty_ReturnsLastName()
    {
        // Arrange
        var user = new ApplicationUser
        {
            FirstName = "",
            LastName = "Doe"
        };

        // Act & Assert
        user.Name.Should().Be("Doe");
    }

    [Fact]
    public void Name_WhenLastNameEmpty_ReturnsFirstName()
    {
        // Arrange
        var user = new ApplicationUser
        {
            FirstName = "John",
            LastName = ""
        };

        // Act & Assert
        user.Name.Should().Be("John");
    }

    [Fact]
    public void FullName_ReturnsSameAsName()
    {
        // Arrange
        var user = new ApplicationUser
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Act & Assert
        user.FullName.Should().Be(user.Name);
    }

    [Fact]
    public void Phone_SetToValidValue_PersistsValue()
    {
        // Arrange
        var user = new ApplicationUser();
        var phone = "555-1234";

        // Act
        user.Phone = phone;

        // Assert
        user.Phone.Should().Be(phone);
    }

    [Fact]
    public void Language_SetToValidValue_PersistsValue()
    {
        // Arrange
        var user = new ApplicationUser();
        var language = "EN";

        // Act
        user.Language = language;

        // Assert
        user.Language.Should().Be(language);
    }

    [Fact]
    public void Region_SetToValidValue_PersistsValue()
    {
        // Arrange
        var user = new ApplicationUser();
        var region = "North America";

        // Act
        user.Region = region;

        // Assert
        user.Region.Should().Be(region);
    }

    [Fact]
    public void Code_SetToValidValue_PersistsValue()
    {
        // Arrange
        var user = new ApplicationUser();
        var code = "ABC123";

        // Act
        user.Code = code;

        // Assert
        user.Code.Should().Be(code);
    }
}
