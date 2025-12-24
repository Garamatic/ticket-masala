using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        _output = output;
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // Bypass strict auth for testing
                services.Configure<AuthorizationOptions>(options => options.FallbackPolicy = null);

                // Remove existing if any (just in case)
                var descriptors = services.Where(d => d.ServiceType == typeof(Microsoft.EntityFrameworkCore.DbContextOptions<TicketMasala.Domain.Data.MasalaDbContext>)).ToList();
                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<TicketMasala.Domain.Data.MasalaDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForFunctionalTesting");
                });

                // Ensure schema is created
                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<TicketMasala.Domain.Data.MasalaDbContext>();
                    db.Database.EnsureCreated();

                    // Seed basic data if empty
                    if (!db.Projects.Any())
                    {
                        db.Projects.Add(new TicketMasala.Domain.Entities.Project
                        {
                            Guid = Guid.NewGuid(),
                            Name = "Seeded Project",
                            Status = TicketMasala.Domain.Common.Status.InProgress,
                            CustomerIds = new List<string>() // Ensure new column is populated
                        });
                        db.SaveChanges();
                    }
                }
            });
        });
        try
        {
            _client = _factory.CreateClient();
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Detailed Error Creating Client: {ex}");
            throw;
        }
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
