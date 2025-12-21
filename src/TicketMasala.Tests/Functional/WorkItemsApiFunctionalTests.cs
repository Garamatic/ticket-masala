using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TicketMasala.Web.ViewModels.Api;
using Xunit;

namespace TicketMasala.Tests.Functional;

public class WorkItemsApiFunctionalTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly JsonSerializerOptions _jsonOptions;

    public WorkItemsApiFunctionalTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task Get_All_WorkItems_Returns_Success_And_Json()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/work-items");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();

        // Should be an array
        var workItems = JsonSerializer.Deserialize<List<WorkItemDto>>(content, _jsonOptions);
        Assert.NotNull(workItems);
    }

    [Fact]
    public async Task Get_NonExistent_WorkItem_Returns_NotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/work-items/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WorkItem_Validates_Required_Fields()
    {
        var client = _factory.CreateClient();
        var invalidItem = new WorkItemDto(); // Missing Title, Description, Status...

        var response = await client.PostAsJsonAsync("/api/v1/work-items", invalidItem);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
