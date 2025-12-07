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
    private readonly Mock<ITicketService> _mockTicketService;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly TicketCommentsController _controller;

    public TicketCommentsControllerTests()
    {
        _mockTicketService = new Mock<ITicketService>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        _controller = new TicketCommentsController(
            _mockTicketService.Object,
            _mockHttpContextAccessor.Object
        );

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
        }, "mock"));

        var context = new DefaultHttpContext { User = user };
        _mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(context);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };
    }

    [Fact]
    public async Task Add_RedirectsToDetail_WithValidComment()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var comment = "This is a comment";
        var isInternal = false;

        // Act
        var result = await _controller.Add(ticketId, comment, isInternal);

        // Assert
        _mockTicketService.Verify(s => s.AddCommentAsync(
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
    public async Task Add_DoesNothing_WhenCommentIsEmpty()
    {
        // Arrange
        var ticketId = Guid.NewGuid();

        // Act
        var result = await _controller.Add(ticketId, "", false);

        // Assert
        _mockTicketService.Verify(s => s.AddCommentAsync(
            It.IsAny<Guid>(), 
            It.IsAny<string>(), 
            It.IsAny<bool>(), 
            It.IsAny<string>()), Times.Never);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);
    }

    [Fact]
    public async Task Add_ReturnsNotFound_WhenTicketServiceThrowsArgumentException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var comment = "Comment";

        _mockTicketService.Setup(s => s.AddCommentAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .ThrowsAsync(new ArgumentException());

        // Act
        var result = await _controller.Add(ticketId, comment, false);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
