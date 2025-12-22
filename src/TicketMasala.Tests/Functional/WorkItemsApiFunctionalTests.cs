using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using TicketMasala.Web.ViewModels.Api;
using TicketMasala.Tests.IntegrationTests;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Data;
using Xunit;

namespace TicketMasala.Tests.Functional;

public class WorkItemTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public WorkItemTestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] {
            new Claim(ClaimTypes.Name, "WorkItem Tester"),
            new Claim(ClaimTypes.NameIdentifier, "work-item-test-user-id"),
            new Claim(ClaimTypes.Role, "Customer")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

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
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, WorkItemTestAuthHandler>("Test", options => { });

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
    }
}
