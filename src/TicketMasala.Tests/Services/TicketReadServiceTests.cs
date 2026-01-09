using TicketMasala.Web.Engine.GERDA.Tickets;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Repositories;
using TicketMasala.Domain.Data;
using TicketMasala.Web.Engine.GERDA.Configuration;
using TicketMasala.Domain.Configuration;

namespace TicketMasala.Tests.Services;

public class TicketReadServiceTests
{
    private readonly DbContextOptions<MasalaDbContext> _dbOptions;

    public TicketReadServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseInMemoryDatabase(databaseName: "TestTicketReadDb_" + Guid.NewGuid())
            .Options;
    }

    [Fact]
    public void ParseCustomFields_ReturnsCorrectJson()
    {
        // Arrange
        var domainConfig = new Mock<IDomainConfigurationService>();
        var ticketRepo = new Mock<ITicketRepository>();
        var logger = new Mock<ILogger<TicketReadService>>();

        var service = new TicketReadService(
            new Mock<MasalaDbContext>(new DbContextOptions<MasalaDbContext>()).Object,
            ticketRepo.Object,
            new Mock<IUserRepository>().Object,
            new Mock<IProjectRepository>().Object,
            new Mock<IAuditService>().Object,
            new Mock<IHttpContextAccessor>().Object,
            logger.Object,
            domainConfig.Object,
            new Mock<TicketMasala.Web.Engine.GERDA.Tickets.Domain.TicketReportingService>(ticketRepo.Object, new Mock<ILogger<TicketMasala.Web.Engine.GERDA.Tickets.Domain.TicketReportingService>>().Object).Object
        );

        // Mock Domain Config
        var fields = new List<CustomFieldDefinition>
            {
                new CustomFieldDefinition { Name = "Budget", Type = "Currency", Label = "Budget" },
                new CustomFieldDefinition { Name = "IsUrgent", Type = "Boolean", Label = "Urgent?" },
                new CustomFieldDefinition { Name = "Notes", Type = "String", Label = "Notes" },
                new CustomFieldDefinition { Name = "TicketType", Type = "String", Label = "Ticket Type" }
            };
        domainConfig.Setup(d => d.GetCustomFields("IT")).Returns(fields);

        var formValues = new Dictionary<string, string>
            {
                { "customFields[TicketType]", "Support" },
                { "customFields[Budget]", "500" },
                { "customFields[IsUrgent]", "true" },
                { "customFields[Notes]", "Some notes" }
            };

        // Act
        var json = service.ParseCustomFields("IT", formValues);

        // Assert
        Assert.Contains("\"TicketType\":\"Support\"", json);
        Assert.Contains("\"Budget\":500", json); // Check numeric parsing
        Assert.Contains("\"IsUrgent\":true", json); // Check boolean parsing
        Assert.Contains("\"Notes\":\"Some notes\"", json);
    }
}
