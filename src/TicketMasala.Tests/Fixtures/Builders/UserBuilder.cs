using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Tests.Fixtures.Builders;

/// <summary>
/// Fluent builder for creating User test objects.
/// </summary>
public class UserBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private string _email = $"user_{Guid.NewGuid():N}@test.com";
    private string _userName = $"user_{Guid.NewGuid():N}@test.com";
    private string _firstName = "Test";
    private string _lastName = "User";
    private string _phone = "555-1234";
    private bool _emailConfirmed = true;

    public UserBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        _userName = email;
        return this;
    }

    public UserBuilder WithUserName(string userName)
    {
        _userName = userName;
        return this;
    }

    public UserBuilder WithName(string firstName, string lastName)
    {
        _firstName = firstName;
        _lastName = lastName;
        return this;
    }

    public UserBuilder WithFirstName(string firstName)
    {
        _firstName = firstName;
        return this;
    }

    public UserBuilder WithLastName(string lastName)
    {
        _lastName = lastName;
        return this;
    }

    public UserBuilder WithPhone(string phone)
    {
        _phone = phone;
        return this;
    }

    public UserBuilder WithEmailConfirmed(bool confirmed)
    {
        _emailConfirmed = confirmed;
        return this;
    }

    public ApplicationUser BuildCustomer()
    {
        return new ApplicationUser
        {
            Id = _id,
            UserName = _userName,
            Email = _email,
            FirstName = _firstName,
            LastName = _lastName,
            Phone = _phone,
            NormalizedEmail = _email.ToUpperInvariant(),
            NormalizedUserName = _userName.ToUpperInvariant(),
            EmailConfirmed = _emailConfirmed
        };
    }

    public Employee BuildEmployee(EmployeeType level = EmployeeType.Support, string team = "Support")
    {
        return new Employee
        {
            Id = _id,
            UserName = _userName,
            Email = _email,
            FirstName = _firstName,
            LastName = _lastName,
            Phone = _phone,
            Team = team,
            Level = level,
            Language = "EN",
            MaxCapacityPoints = 40,
            NormalizedEmail = _email.ToUpperInvariant(),
            NormalizedUserName = _userName.ToUpperInvariant(),
            EmailConfirmed = _emailConfirmed
        };
    }

    public Employee BuildProjectManager(string team = "IT")
    {
        return BuildEmployee(EmployeeType.ProjectManager, team);
    }

    public Employee BuildSupport(string team = "Support")
    {
        return BuildEmployee(EmployeeType.Support, team);
    }

    public Employee BuildDeveloper(string team = "Engineering")
    {
        return BuildEmployee(EmployeeType.Developer, team);
    }
}
