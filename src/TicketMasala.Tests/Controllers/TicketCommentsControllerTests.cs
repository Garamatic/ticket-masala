using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System.Security.Claims;
using TicketMasala.Web.Controllers;
using TicketMasala.Web.Engine.GERDA.Tickets;
using Xunit;

namespace TicketMasala.Tests.Controllers;

public class TicketCommentsControllerTests
{
    private readonly Mock<ITicketWorkflowService> _mockTicketWorkflowService;
    private readonly TicketCommentsController _controller;

    public TicketCommentsControllerTests()
    {
        _mockTicketWorkflowService = new Mock<ITicketWorkflowService>();
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<TicketCommentsController>>();

        _controller = new TicketCommentsController(_mockTicketWorkflowService.Object, mockLogger.Object);

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
        _mockTicketWorkflowService.Verify(s => s.AddCommentAsync(
            ticketId,
            comment,
            isInternal,
            "test-user-id"), Times.Once);

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
        _mockTicketWorkflowService.Verify(s => s.AddCommentAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<string>()), Times.Never);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);
    }

    [Fact]
    public async Task RequestReview_CallsService()
    {
        // Arrange
        var ticketGuid = Guid.NewGuid();

        // Act
        var result = await _controller.RequestReview(ticketGuid);

        // Assert
        _mockTicketWorkflowService.Verify(s => s.RequestReviewAsync(ticketGuid, "test-user-id"), Times.Once);

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

        // Act
        var result = await _controller.SubmitReview(ticketGuid, score, feedback, approve);

        // Assert
        _mockTicketWorkflowService.Verify(s => s.SubmitReviewAsync(
            ticketGuid,
            score,
            feedback,
            approve,
            "test-user-id"), Times.Once);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);
    }
}
