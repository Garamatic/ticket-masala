using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Controllers;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.Core;
using Xunit;

namespace TicketMasala.Tests.Controllers;

public class TicketAttachmentsControllerTests : IDisposable
{
    private readonly Mock<IFileStorageService> _mockFileStorage;
    private readonly Mock<ILogger<TicketAttachmentsController>> _mockLogger;
    private readonly Mock<ISystemClock> _mockClock;
    private readonly MasalaDbContext _context;
    private readonly TicketAttachmentsController _controller;

    public TicketAttachmentsControllerTests()
    {
        _mockFileStorage = new Mock<IFileStorageService>();
        _mockLogger = new Mock<ILogger<TicketAttachmentsController>>();
        _mockClock = new Mock<ISystemClock>();

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
            _mockFileStorage.Object,
            _context,
            _mockLogger.Object,
            _mockClock.Object
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

        _mockFileStorage.Setup(s => s.StoreFileAsync(It.IsAny<Stream>(), "test.txt"))
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
        _mockFileStorage.Setup(s => s.RetrieveFileAsync("stored.txt"))
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
