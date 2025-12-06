using TicketMasala.Web.Models;
using Bogus;

namespace TicketMasala.Tests.TestHelpers;

/// <summary>
/// Test data builder using Bogus library for generating realistic test data
/// </summary>
public static class TestDataBuilder
{
    private static readonly Faker _faker = new Faker();

    public static ApplicationUser BuildCustomer(string? email = null, string? id = null)
    {
        return new ApplicationUser
        {
            Id = id ?? Guid.NewGuid().ToString(),
            UserName = email ?? _faker.Internet.Email(),
            Email = email ?? _faker.Internet.Email(),
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            Phone = _faker.Phone.PhoneNumber(),
            Code = _faker.Random.AlphaNumeric(8).ToUpper()
        };
    }

    public static Employee BuildEmployee(string? email = null, EmployeeType level = EmployeeType.Support)
    {
        return new Employee
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email ?? _faker.Internet.Email(),
            Email = email ?? _faker.Internet.Email(),
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            Phone = _faker.Phone.PhoneNumber(),
            Team = _faker.Commerce.Department(),
            Level = level,
            Language = "EN",
            Specializations = "[\"Support\",\"Troubleshooting\"]",
            MaxCapacityPoints = _faker.Random.Int(30, 50),
            Region = _faker.Address.City()
        };
    }

    public static Ticket BuildTicket(ApplicationUser? customer = null, Employee? responsible = null, Status status = Status.Pending)
    {
        return new Ticket
        {
            Guid = Guid.NewGuid(),
            Description = _faker.Lorem.Sentence(),
            TicketStatus = status,
            TicketType = TicketType.ProjectRequest,
            Customer = customer ?? BuildCustomer(),
            Responsible = responsible,
            CreatorGuid = customer != null ? Guid.Parse(customer.Id) : Guid.NewGuid(),
            ResponsibleId = responsible?.Id,
            CompletionTarget = DateTime.UtcNow.AddDays(_faker.Random.Int(1, 30)),
            EstimatedEffortPoints = _faker.Random.Int(1, 13),
            PriorityScore = _faker.Random.Double(0, 100)
        };
    }

    public static Project BuildProject(ApplicationUser? customer = null, Employee? projectManager = null)
    {
        var project = new Project
        {
            Guid = Guid.NewGuid(),
            Name = _faker.Commerce.ProductName(),
            Description = _faker.Lorem.Paragraph(),
            Status = Status.InProgress,
            Customer = customer ?? BuildCustomer(),
            ProjectManager = projectManager,
            CompletionTarget = DateTime.UtcNow.AddMonths(_faker.Random.Int(1, 6)),
            CreatorGuid = Guid.NewGuid()
        };

        if (customer != null)
        {
            project.Customers.Add(customer);
        }

        return project;
    }

    public static ProjectTemplate BuildProjectTemplate()
    {
        return new ProjectTemplate
        {
            Guid = Guid.NewGuid(),
            Name = _faker.Commerce.ProductName() + " Template",
            Description = _faker.Lorem.Paragraph(),
            EstimatedDuration = _faker.Random.Int(30, 180)
        };
    }

    public static QualityReview BuildQualityReview(Guid ticketId, string? reviewerId = null)
    {
        return new QualityReview
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            ReviewerId = reviewerId ?? Guid.NewGuid().ToString(),
            Comments = _faker.Lorem.Paragraph(),
            Feedback = _faker.Lorem.Sentence(),
            Score = _faker.Random.Int(0, 100),
            CreatedAt = DateTime.UtcNow,
            ReviewDate = DateTime.UtcNow
        };
    }
}
