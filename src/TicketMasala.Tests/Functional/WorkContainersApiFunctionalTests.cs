using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
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
                var authServiceMock = new Mock<IAuthenticationService>();
                authServiceMock
                    .Setup(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
                    .ReturnsAsync((HttpContext context, string scheme) =>
                    {
                        var claims = new[] {
                            new Claim(ClaimTypes.Name, "WorkContainer Tester"),
                            new Claim(ClaimTypes.NameIdentifier, "work-container-test-user-id"),
                            new Claim(ClaimTypes.Role, "Customer")
                        };
                        var identity = new ClaimsIdentity(claims, scheme);
                        var principal = new ClaimsPrincipal(identity);
                        var ticket = new AuthenticationTicket(principal, scheme);
                        return AuthenticateResult.Success(ticket);
                    });
                services.AddScoped(sp => authServiceMock.Object);

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
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
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
