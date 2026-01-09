using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TicketMasala.Web.Controllers;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Engine.Ingestion.Background;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets;
using Xunit;

namespace TicketMasala.Tests.Controllers;

public class ImportControllerTests
{
    private readonly Mock<ITicketImportService> _mockImportService;
    private readonly Mock<ITicketService> _mockTicketService;
    private readonly Mock<ILogger<ImportController>> _mockLogger;
    private readonly Mock<IBackgroundTaskQueue> _mockTaskQueue;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly ImportController _controller;

    public ImportControllerTests()
    {
        _mockImportService = new Mock<ITicketImportService>();
        _mockTicketService = new Mock<ITicketService>();
        _mockLogger = new Mock<ILogger<ImportController>>();
        _mockTaskQueue = new Mock<IBackgroundTaskQueue>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockFileStorageService = new Mock<IFileStorageService>();

        _controller = new ImportController(
            _mockImportService.Object,
            _mockTicketService.Object,
            _mockLogger.Object,
            _mockTaskQueue.Object,
            _mockScopeFactory.Object,
            _mockFileStorageService.Object
        );

        // Setup TempData
        _controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
    }

    [Fact]
    public async Task Upload_ReturnsRedirect_WhenFileIsNull()
    {
        // Act
        var result = await _controller.Upload(null!);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Please select a file.", _controller.TempData["Error"]);
    }

    [Fact]
    public async Task Upload_ReturnsMapFieldsView_WhenFileIsValid()
    {
        // Arrange
        var content = "Header1,Header2\nVal1,Val2";
        var fileName = "test.csv";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = new FormFile(stream, 0, stream.Length, "file", fileName);
        var fileId = Guid.NewGuid().ToString();

        var parsedRows = new List<dynamic>
        {
            new Dictionary<string, object> { { "Header1", "Val1" }, { "Header2", "Val2" } }
        };

        _mockImportService.Setup(s => s.ParseFile(It.IsAny<Stream>(), fileName))
            .Returns(parsedRows);
        
        _mockFileStorageService.Setup(s => s.StoreFileAsync(It.IsAny<Stream>(), fileName))
            .ReturnsAsync(fileId);

        // Act
        var result = await _controller.Upload(file);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("MapFields", viewResult.ViewName);
        var model = Assert.IsAssignableFrom<List<string>>(viewResult.Model);
        Assert.Contains("Header1", model);
        Assert.Equal(fileId, _controller.TempData["FileId"]);
    }

    [Fact]
    public async Task ExecuteImport_ReturnsRedirect_WhenMappingIsNull()
    {
        // Act
        var result = await _controller.ExecuteImport(null!);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("No field mapping provided.", _controller.TempData["Error"]);
    }

    [Fact]
    public async Task ExecuteImport_ReturnsRedirect_WhenMappingIsEmpty()
    {
        // Act
        var result = await _controller.ExecuteImport(new Dictionary<string, string>());

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("No field mapping provided.", _controller.TempData["Error"]);
    }

    [Fact]
    public async Task ExecuteImport_ReturnsRedirect_WhenFileIdIsMissing()
    {
        // Arrange
        var mapping = new Dictionary<string, string> { { "Header1", "Field1" } };
        _controller.TempData["FileId"] = null;

        // Act
        var result = await _controller.ExecuteImport(mapping);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Session expired. Please upload again.", _controller.TempData["Error"]);
    }
}
