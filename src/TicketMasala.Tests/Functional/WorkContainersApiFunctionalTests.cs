using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TicketMasala.Web.ViewModels.Api;
using Xunit;

namespace TicketMasala.Tests.Functional;

public class WorkContainersApiFunctionalTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Xunit.Abstractions.ITestOutputHelper _output;

    public WorkContainersApiFunctionalTests(WebApplicationFactory<Program> factory, Xunit.Abstractions.ITestOutputHelper output)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task Get_All_WorkContainers_Returns_Success_And_Json()
    {
        // Arrange
        // (Assumes fresh DB or seeded data, but even empty list is Success)

        // Act
        var response = await _client.GetAsync("/api/v1/work-containers");

        // Assert
        if (!response.IsSuccessStatusCode)
        {
            var contentStr = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Server Error Response: {contentStr}");
        }
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.NotNull(content);
        // Can try to deserialize to verify schema
        var containers = JsonSerializer.Deserialize<List<WorkContainerDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(containers);
    }

    [Fact]
    public async Task Get_NonExistent_WorkContainer_Returns_NotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/work-containers/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WorkContainer_Validates_Required_Fields()
    {
        // Arrange
        var invalidContainer = new WorkContainerDto(); // Missing Name, Status

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/work-containers", invalidContainer);

        // Assert
        // Expect 400 Bad Request due to [Required] attributes on DTO
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
