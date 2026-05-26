using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using TicketMasala.Web.Controllers;
using TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;
using Xunit;

namespace TicketMasala.Tests.Controllers;

public class TicketCommentsControllerTests
{
    private readonly Mock<ITicketLifecycle> _mockTicketLifecycle;
    private readonly TicketCommentsController _controller;

    public TicketCommentsControllerTests()
    {
        _mockTicketLifecycle = new Mock<ITicketLifecycle>();
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<TicketCommentsController>>();

        _controller = new TicketCommentsController(_mockTicketLifecycle.Object, mockLogger.Object);

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

        _mockTicketLifecycle.Setup(x => x.ExecuteAsync(
            It.Is<AddCommentCommand>(c => c.TicketGuid == ticketId && c.Body == comment && c.IsInternal == isInternal),
            It.Is<TicketContext>(ctx => ctx.UserId == "test-user-id")))
            .ReturnsAsync(new TicketResult { Success = true, Comment = new TicketMasala.Domain.Entities.TicketComment { Body = comment } });

        // Act
        var result = await _controller.AddComment(ticketId, comment, isInternal);

        // Assert
        _mockTicketLifecycle.Verify(x => x.ExecuteAsync(
            It.Is<AddCommentCommand>(c => c.TicketGuid == ticketId && c.Body == comment && c.IsInternal == isInternal),
            It.Is<TicketContext>(ctx => ctx.UserId == "test-user-id")), Times.Once);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);
        Assert.Equal("Ticket", redirect.ControllerName);
        Assert.Equal(ticketId, redirect.RouteValues?["id"]);
    }

    [Fact]
    public async Task AddComment_DoesNotAdd_WhenCommentIsEmpty()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var result = await _controller.AddComment(ticketId, "", false);

        // Assert
        _mockTicketLifecycle.Verify(x => x.ExecuteAsync(
            It.IsAny<AddCommentCommand>(),
            It.IsAny<TicketContext>()), Times.Never);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);
    }

    [Fact]
    public async Task RequestReview_CallsService()
    {
        // Arrange
        var ticketGuid = Guid.NewGuid();

        _mockTicketLifecycle.Setup(x => x.ExecuteAsync(
            It.Is<RequestReviewCommand>(c => c.TicketGuid == ticketGuid),
            It.Is<TicketContext>(ctx => ctx.UserId == "test-user-id")))
            .ReturnsAsync(new TicketResult { Success = true });

        // Act
        var result = await _controller.RequestReview(ticketGuid);

        // Assert
        _mockTicketLifecycle.Verify(x => x.ExecuteAsync(
            It.Is<RequestReviewCommand>(c => c.TicketGuid == ticketGuid),
            It.Is<TicketContext>(ctx => ctx.UserId == "test-user-id")), Times.Once);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);
    }

    [Fact]
    public async Task SubmitReview_CallsService_WithCorrectParameters()
    {
        // Arrange
        var ticketGuid = Guid.NewGuid();
        var score = 5;
        var feedback = "Great work!";
        var approve = true;

        _mockTicketLifecycle.Setup(x => x.ExecuteAsync(
            It.Is<SubmitReviewCommand>(c => c.TicketGuid == ticketGuid && c.Score == score && c.Feedback == feedback && c.Approved == approve),
            It.Is<TicketContext>(ctx => ctx.UserId == "test-user-id")))
            .ReturnsAsync(new TicketResult { Success = true });

        // Act
        var result = await _controller.SubmitReview(ticketGuid, score, feedback, approve);

        // Assert
        _mockTicketLifecycle.Verify(x => x.ExecuteAsync(
            It.Is<SubmitReviewCommand>(c => c.TicketGuid == ticketGuid && c.Score == score && c.Feedback == feedback && c.Approved == approve),
            It.Is<TicketContext>(ctx => ctx.UserId == "test-user-id")), Times.Once);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);
    }
}
