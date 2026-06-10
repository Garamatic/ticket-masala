using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Tests.IntegrationTests;
using TicketMasala.Web.ViewModels.Api;
using Xunit;

namespace TicketMasala.Tests.Functional;

public class WorkItemsApiFunctionalTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions;

    public WorkItemsApiFunctionalTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private HttpClient CreateAuthenticatedClient()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var authServiceMock = new Mock<IAuthenticationService>();
                authServiceMock
                    .Setup(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
                    .ReturnsAsync((HttpContext context, string scheme) =>
                    {
                        var claims = new[] {
                            new Claim(ClaimTypes.Name, "WorkItem Tester"),
                            new Claim(ClaimTypes.NameIdentifier, "work-item-test-user-id"),
                            new Claim(ClaimTypes.Role, "Customer")
                        };
                        var identity = new ClaimsIdentity(claims, scheme);
                        var principal = new ClaimsPrincipal(identity);
                        var ticket = new AuthenticationTicket(principal, scheme);
                        return AuthenticateResult.Success(ticket);
                    });
                services.AddScoped(sp => authServiceMock.Object);

                // Ensure test user exists
                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<MasalaDbContext>();
                    var testUserId = "work-item-test-user-id";

                    if (!db.Users.Any(u => u.Id == testUserId))
                    {
                        try
                        {
                            db.Users.Add(new ApplicationUser
                            {
                                Id = testUserId,
                                UserName = "workitem.test",
                                Email = "workitem@example.com",
                                FirstName = "WorkItem",
                                LastName = "Tester",
                                PhoneNumber = "555-0200",
                                Phone = "555-0200"
                            });
                            db.SaveChanges();
                        }
                        catch (ArgumentException)
                        {
                            // Ignore if already exists (concurrency race)
                        }
                    }
                }
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Get_All_WorkItems_Returns_Success_And_Json()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

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
        var client = CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        var response = await client.GetAsync($"/api/v1/work-items/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WorkItem_Validates_Required_Fields()
    {
        var client = CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        var invalidItem = new WorkItemDto(); // Missing Title, Description, Status...

        var response = await client.PostAsJsonAsync("/api/v1/work-items", invalidItem);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(_jsonOptions);
        Assert.NotNull(problem);
        Assert.True(problem!.Errors.ContainsKey("Title"));
        Assert.True(problem.Errors.ContainsKey("Description"));
        Assert.True(problem.Errors.ContainsKey("Status"));
        Assert.True(problem.Errors.ContainsKey("DomainId"));
    }

    [Fact]
    public async Task Create_WorkItem_WithTooLongFields_Returns_BadRequest_With_ValidationErrors()
    {
        var client = CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        var invalidItem = new WorkItemDto
        {
            Title = new string('A', 201),
            Description = new string('B', 5001),
            Status = "New",
            DomainId = new string('C', 51)
        };

        var response = await client.PostAsJsonAsync("/api/v1/work-items", invalidItem);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(_jsonOptions);
        Assert.NotNull(problem);
        Assert.True(problem!.Errors.ContainsKey("Title"));
        Assert.True(problem.Errors.ContainsKey("Description"));
        Assert.True(problem.Errors.ContainsKey("DomainId"));
    }

    [Fact]
    public async Task Create_WorkItem_WithMaxLengthFields_Succeeds()
    {
        var client = CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        var validItem = new WorkItemDto
        {
            Title = new string('A', 200),
            Description = new string('B', 5000),
            Status = "New",
            DomainId = "IT"
        };

        var response = await client.PostAsJsonAsync("/api/v1/work-items", validItem);

        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var created = await response.Content.ReadFromJsonAsync<WorkItemDto>(_jsonOptions);
            Assert.NotNull(created);
            Assert.Equal(validItem.Title, created!.Title);
            Assert.Equal(validItem.Description, created.Description);
            Assert.Equal(validItem.DomainId, created.DomainId);
        }
    }

    [Fact]
    public async Task Create_WorkItem_Unauthenticated_User_Is_Unauthorized_Or_Redirected()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var item = new WorkItemDto
        {
            Title = "Test",
            Description = "Test description",
            Status = "New",
            DomainId = "IT"
        };

        var response = await client.PostAsJsonAsync("/api/v1/work-items", item);

        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Redirect);
    }
}
