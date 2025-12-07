using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using TicketMasala.Web.Controllers;
using TicketMasala.Web.Engine.Ingestion;
using TicketMasala.Web.Services.Tickets;
using Xunit;

namespace TicketMasala.Tests.Controllers;

public class ImportControllerTests
{
    private readonly Mock<ITicketImportService> _mockImportService;
    private readonly Mock<ITicketService> _mockTicketService;
    private readonly Mock<ILogger<ImportController>> _mockLogger;
    private readonly ImportController _controller;

    public ImportControllerTests()
    {
        _mockImportService = new Mock<ITicketImportService>();
        _mockTicketService = new Mock<ITicketService>();
        _mockLogger = new Mock<ILogger<ImportController>>();

        _controller = new ImportController(
            _mockImportService.Object,
            _mockTicketService.Object,
            _mockLogger.Object
        );

        // Setup TempData
        _controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
    }

    [Fact]
    public void Upload_ReturnsRedirect_WhenFileIsNull()
    {
        // Act
        var result = _controller.Upload(null!);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Please select a file.", _controller.TempData["Error"]);
    }

    [Fact]
    public void Upload_ReturnsMapFieldsView_WhenFileIsValid()
    {
        // Arrange
        var content = "Header1,Header2\nVal1,Val2";
        var fileName = "test.csv";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = new FormFile(stream, 0, stream.Length, "file", fileName);

        var parsedRows = new List<dynamic>
        {
            new Dictionary<string, object> { { "Header1", "Val1" }, { "Header2", "Val2" } }
        };

        _mockImportService.Setup(s => s.ParseFile(It.IsAny<Stream>(), fileName))
            .Returns(parsedRows);

        // Act
        var result = _controller.Upload(file);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("MapFields", viewResult.ViewName);
        var model = Assert.IsAssignableFrom<List<string>>(viewResult.Model);
        Assert.Contains("Header1", model);
    }
}
