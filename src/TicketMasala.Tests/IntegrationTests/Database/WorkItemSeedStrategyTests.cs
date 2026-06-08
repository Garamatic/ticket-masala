using TicketMasala.Domain.Entities;
using TicketMasala.Tests.TestHelpers;
using Xunit;

namespace TicketMasala.Tests.IntegrationTests.Database;

[Collection("Database")]
public class WorkItemSeedStrategyTests
{
    private readonly DatabaseTestFixture _fixture;

    public WorkItemSeedStrategyTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Ticket_Create_And_Comment_Save_Successfully()
    {
        var customer = await _fixture.SeedTestCustomerAsync();

        var ticket = Ticket.Create("Test description", "Test title", customer.Id, "IT", null, "Incident");

        _fixture.Context.Tickets.Add(ticket);
        await _fixture.Context.SaveChangesAsync();

        var comment = new TicketComment
        {
            TicketId = ticket.Guid,
            Body = "Test comment",
            CreatedAt = DateTime.UtcNow,
            AuthorId = customer.Id
        };

        _fixture.Context.TicketComments.Add(comment);
        await _fixture.Context.SaveChangesAsync();

        var savedComment = await _fixture.Context.TicketComments.FindAsync(comment.Id);
        Assert.NotNull(savedComment);
        Assert.Equal(ticket.Guid, savedComment.TicketId);
    }
}
