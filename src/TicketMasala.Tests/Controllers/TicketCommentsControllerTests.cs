using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using TicketMasala.Web.Controllers;
using TicketMasala.Web.Data;
using TicketMasala.Web.Models;
using Xunit;

namespace TicketMasala.Tests.Controllers;

public class TicketCommentsControllerTests : IDisposable
{
    private readonly MasalaDbContext _context;
    private readonly TicketCommentsController _controller;

    public TicketCommentsControllerTests()
    {
        // Use in-memory database for testing
        var options = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MasalaDbContext(options);

        // Create a mock logger
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<TicketCommentsController>>();

        _controller = new TicketCommentsController(_context, mockLogger.Object);

        // Set up user claims
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
        }, "mock"));

        var context = new DefaultHttpContext { User = user };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };

        // Set up TempData
        _controller.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());
    }

    [Fact]
    public async Task AddComment_RedirectsToDetail_WithValidComment()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var comment = "This is a comment";
        var isInternal = false;

        // Act
        var result = await _controller.AddComment(ticketId, comment, isInternal);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);
        Assert.Equal("Ticket", redirect.ControllerName);
        Assert.Equal(ticketId, redirect.RouteValues?["id"]);

        // Verify comment was added
        var addedComment = await _context.TicketComments.FirstOrDefaultAsync(c => c.TicketId == ticketId);
        Assert.NotNull(addedComment);
        Assert.Equal(comment, addedComment.Body);
        Assert.Equal(isInternal, addedComment.IsInternal);
    }

    [Fact]
    public async Task AddComment_DoesNotAdd_WhenCommentIsEmpty()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var result = await _controller.AddComment(ticketId, "", false);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);

        // Verify comment was not added
        var comment = await _context.TicketComments.FirstOrDefaultAsync(c => c.TicketId == ticketId);
        Assert.Null(comment);
    }

    [Fact]
    public async Task RequestReview_SetsStatusToPending()
    {
        // Arrange
        var ticketGuid = Guid.NewGuid();
        var ticket = new Ticket
        {
            Guid = ticketGuid,
            Description = "Test ticket",
            CreationDate = DateTime.UtcNow,
            TicketStatus = Status.Pending,
            ReviewStatus = ReviewStatus.None,
            CustomerId = "test-customer-id"
        };
        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.RequestReview(ticketGuid);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);

        // Verify review status was updated
        var updatedTicket = await _context.Tickets.FirstOrDefaultAsync(t => t.Guid == ticketGuid);
        Assert.NotNull(updatedTicket);
        Assert.Equal(ReviewStatus.Pending, updatedTicket.ReviewStatus);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
