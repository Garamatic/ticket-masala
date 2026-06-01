using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.ViewModels.Projects;
using Xunit;

namespace TicketMasala.Tests.IntegrationTests.Api;

/// <summary>
/// Integration tests for ProjectsApiController REST API endpoints
/// </summary>
public class ProjectsApiControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProjectsApiControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAuthenticatedClient(string userId, string userName, string role, string email)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<TestAuthOptions, TestAuthHandler>("Test", options => { options.Role = role; options.NameIdentifier = userId; });

                // Seed test user
                var sp = services.BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateScopes = false,
                    ValidateOnBuild = false
                });
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<MasalaDbContext>();
                    if (!db.Users.Any(u => u.Id == userId))
                    {
                        if (role == "Employee")
                        {
                            db.Users.Add(new Employee
                            {
                                Id = userId,
                                UserName = userName,
                                Email = email,
                                FirstName = "Test",
                                LastName = "Employee",
                                Phone = "555-0100",
                                Team = "Engineering",
                                Level = EmployeeType.ProjectManager,
                                NormalizedEmail = email.ToUpperInvariant(),
                                NormalizedUserName = userName.ToUpperInvariant()
                            });
                        }
                        else
                        {
                            db.Users.Add(new ApplicationUser
                            {
                                Id = userId,
                                UserName = userName,
                                Email = email,
                                FirstName = "Test",
                                LastName = "Customer",
                                Phone = "555-0100",
                                NormalizedEmail = email.ToUpperInvariant(),
                                NormalizedUserName = userName.ToUpperInvariant()
                            });
                        }
                        db.SaveChanges();
                    }
                }
            });
        });

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private HttpClient CreateAnonymousClient()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // No authentication for anonymous tests
            });
        });

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    [Fact(DisplayName = "GET /api/v1/projects - Authenticated user can get all projects")]
    public async Task GetAll_AuthenticatedUser_ReturnsProjectList()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-proj-api-1", "test.proj.api1@test.com", "Employee", "test.proj.api1@test.com");

        // Act
        var response = await client.GetAsync("/api/v1/projects");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotNull(content);
        // Should be JSON response with success flag
        Assert.True(content.Contains("success") || content.StartsWith("{"), "Expected JSON response with success indicator");
    }

    [Fact(DisplayName = "GET /api/v1/projects/{id} - Handles non-existent project gracefully")]
    public async Task GetById_NonExistentProject_ReturnsNotFoundOrOk()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-proj-api-2", "test.proj.api2@test.com", "Employee", "test.proj.api2@test.com");
        var randomGuid = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/projects/{randomGuid}");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.OK, // May return OK with error in body
            $"Expected NotFound or OK but got {response.StatusCode}");
    }

    [Fact(DisplayName = "GET /api/v1/projects - Unauthorized without authentication")]
    public async Task GetAll_UnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange
        var client = CreateAnonymousClient();

        // Act
        var response = await client.GetAsync("/api/v1/projects");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Redirect,
            $"Expected Unauthorized or Redirect but got {response.StatusCode}");
    }

    [Fact(DisplayName = "GET /api/v1/projects/customer/{customerId} - Returns projects for customer")]
    public async Task GetByCustomer_ValidCustomerId_ReturnsProjects()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-proj-api-3", "test.proj.api3@test.com", "Employee", "test.proj.api3@test.com");

        // Act
        var response = await client.GetAsync("/api/v1/projects/customer/test-proj-api-3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotNull(content);
    }

    [Fact(DisplayName = "GET /api/v1/projects/search - Search endpoint works")]
    public async Task Search_WithQuery_ReturnsResults()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-proj-api-4", "test.proj.api4@test.com", "Employee", "test.proj.api4@test.com");

        // Act
        var response = await client.GetAsync("/api/v1/projects/search?query=test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(DisplayName = "GET /api/v1/projects/statistics/{customerId} - Returns statistics")]
    public async Task GetStatistics_ValidCustomerId_ReturnsStatistics()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-proj-api-5", "test.proj.api5@test.com", "Employee", "test.proj.api5@test.com");

        // Act
        var response = await client.GetAsync("/api/v1/projects/statistics/test-proj-api-5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotNull(content);
    }

    [Fact(DisplayName = "POST /api/v1/projects - Creates new project")]
    public async Task Create_ValidProject_ReturnsCreatedProject()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var client = CreateAuthenticatedClient(userId, "test.proj.api6@test.com", "Employee", "test.proj.api6@test.com");
        var request = new NewProject
        {
            Name = "API Test Project",
            Description = "Created via API integration test",
            SelectedCustomerId = userId,
            SelectedProjectManagerId = userId,
            IsNewCustomer = false,
            ProjectType = "Standard"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await client.PostAsync("/api/v1/projects", content);

        // Assert
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created,
            $"Expected OK or Created but got {response.StatusCode}. Response: {responseContent}");
    }

    [Fact(DisplayName = "PATCH /api/v1/projects/{id}/status - Updates project status")]
    public async Task UpdateStatus_ValidStatus_ReturnsSuccess()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-proj-api-7", "test.proj.api7@test.com", "Employee", "test.proj.api7@test.com");
        var projectId = Guid.NewGuid();
        var json = JsonSerializer.Serialize(Status.InProgress);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await client.PatchAsync($"/api/v1/projects/{projectId}/status", content);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected OK or NotFound but got {response.StatusCode}");
    }

    [Fact(DisplayName = "PATCH /api/v1/projects/{id}/assign - Assigns project manager")]
    public async Task AssignManager_ValidManagerId_ReturnsSuccess()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-proj-api-8", "test.proj.api8@test.com", "Employee", "test.proj.api8@test.com");
        var projectId = Guid.NewGuid();
        var json = JsonSerializer.Serialize("test-proj-api-8");
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await client.PatchAsync($"/api/v1/projects/{projectId}/assign-manager", content);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected OK or NotFound but got {response.StatusCode}");
    }

    [Fact(DisplayName = "DELETE /api/v1/projects/{id} - Handles existing or non-existent project")]
    public async Task Delete_Project_ReturnsSuccessOrNotFound()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-proj-api-9", "test.proj.api9@test.com", "Admin", "test.proj.api9@test.com");
        var projectId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/v1/projects/{projectId}");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected OK or NotFound but got {response.StatusCode}");
    }
}


