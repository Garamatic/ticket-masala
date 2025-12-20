using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using TicketMasala.Web.Controllers;
using TicketMasala.Web.Data;
using TicketMasala.Web.Models;
using TicketMasala.Web.Engine.Core;
using Xunit;

namespace TicketMasala.Tests.Controllers;

public class TicketAttachmentsControllerTests : IDisposable
{
    private readonly Mock<IFileService> _mockFileService;
    private readonly Mock<ILogger<TicketAttachmentsController>> _mockLogger;
    private readonly MasalaDbContext _context;
    private readonly TicketAttachmentsController _controller;

    public TicketAttachmentsControllerTests()
    {
        _mockFileService = new Mock<IFileService>();
        _mockLogger = new Mock<ILogger<TicketAttachmentsController>>();

        var options = new DbContextOptionsBuilder<MasalaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new MasalaDbContext(options);

        // Seed a user for upload context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
        }, "mock"));

        _controller = new TicketAttachmentsController(
            _mockFileService.Object,
            _context,
            _mockLogger.Object
        );

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        _controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
    }

    [Fact]
    public async Task Upload_ReturnsRedirect_WhenFileIsNull()
    {
        // Act
        var result = await _controller.Upload(Guid.NewGuid(), null!, false);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);
        Assert.Equal("Ticket", redirect.ControllerName);
        Assert.Equal("Please select a file to upload.", _controller.TempData["Error"]);
    }

    [Fact]
    public async Task Upload_Success_SavesFileAndDocument()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var fileName = "test.txt";
        var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
        var file = new FormFile(fileStream, 0, fileStream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        _mockFileService.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>(), "tickets"))
            .ReturnsAsync("stored_filename.txt");

        // Act
        var result = await _controller.Upload(ticketId, file, true);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Detail", redirect.ActionName);

        var doc = await _context.Documents.FirstOrDefaultAsync(d => d.TicketId == ticketId);
        Assert.NotNull(doc);
        Assert.Equal("test.txt", doc.FileName);
        Assert.Equal("stored_filename.txt", doc.StoredFileName);
        Assert.True(doc.IsPublic);
        Assert.Equal("test-user-id", doc.UploaderId);
    }

    [Fact]
    public async Task Download_ReturnsNotFound_WhenDocumentDoesNotExist()
    {
        // Act
        var result = await _controller.Download(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Download_ReturnsFile_WhenDocumentExists()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var document = new Document
        {
            Id = docId,
            FileName = "test.txt",
            StoredFileName = "stored.txt",
            ContentType = "text/plain",
            UploadDate = DateTime.UtcNow
        };
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
        _mockFileService.Setup(s => s.GetFileStreamAsync("stored.txt", "tickets"))
            .ReturnsAsync(stream);

        // Act
        var result = await _controller.Download(docId);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("text/plain", fileResult.ContentType);
        Assert.Equal("test.txt", fileResult.FileDownloadName);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
