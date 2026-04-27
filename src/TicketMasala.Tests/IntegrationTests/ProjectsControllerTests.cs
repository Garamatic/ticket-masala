using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Web;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.GERDA.Configuration;
using Xunit;

namespace TicketMasala.Tests.IntegrationTests;

/// <summary>
/// Integration tests for ProjectsController - tests project CRUD operations
/// </summary>
public class ProjectsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProjectsControllerTests(CustomWebApplicationFactory factory)
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
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

                // Mock IDomainUiService
                var mockDomainUi = new Moq.Mock<IDomainUiService>();
                mockDomainUi.Setup(x => x.GetLabel(Moq.It.IsAny<string>(), Moq.It.IsAny<string>())).Returns((string k, string d) => k);
                mockDomainUi.Setup(x => x.GetIcon(Moq.It.IsAny<string>(), Moq.It.IsAny<string>())).Returns("bi-box");
                mockDomainUi.Setup(x => x.GetDomainCssClass(Moq.It.IsAny<string>())).Returns("theme-default");
                services.AddSingleton(mockDomainUi.Object);

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
        return client;
    }

    [Fact(DisplayName = "GET /Projects - Returns project list for authenticated employee")]
    public async Task Index_AuthenticatedEmployee_ReturnsProjectList()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-proj-emp-1", "test.proj.emp1@test.com", "Employee", "test.proj.emp1@test.com");

        // Act
        var response = await client.GetAsync("/Projects");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("project", content.ToLower());
    }

    [Fact(DisplayName = "GET /Projects/NewProject - Returns create form for employee")]
    public async Task NewProject_Get_Employee_ReturnsCreateForm()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-proj-emp-2", "test.proj.emp2@test.com", "Employee", "test.proj.emp2@test.com");

        // Act
        var response = await client.GetAsync("/Projects/NewProject");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Create", content);
        Assert.Contains("Project", content);
    }

    [Fact(DisplayName = "GET /Projects/Details/{id} - Returns 404 for non-existent project")]
    public async Task Details_NonExistentProject_ReturnsNotFound()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-proj-emp-3", "test.proj.emp3@test.com", "Employee", "test.proj.emp3@test.com");
        var nonExistentGuid = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/Projects/Details/{nonExistentGuid}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "GET /Projects/Edit/{id} - Returns 404 for non-existent project")]
    public async Task Edit_Get_NonExistentProject_ReturnsNotFound()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-proj-emp-4", "test.proj.emp4@test.com", "Employee", "test.proj.emp4@test.com");
        var nonExistentGuid = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/Projects/Edit/{nonExistentGuid}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }



    [Fact(DisplayName = "GET /Projects - Customer can access project list")]
    public async Task Index_Customer_ReturnsProjectList()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-proj-cust-1", "test.proj.cust1@test.com", "Customer", "test.proj.cust1@test.com");

        // Act
        var response = await client.GetAsync("/Projects");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(DisplayName = "Unauthorized user cannot access Projects Index")]
    public async Task Index_UnauthorizedUser_ReturnsUnauthorizedOrRedirect()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // No authentication setup
            });
        });
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/Projects");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Redirect,
            $"Expected Unauthorized or Redirect but got {response.StatusCode}");
    }





    private static string? ExtractAntiforgeryToken(string html)
    {
        if (string.IsNullOrEmpty(html))
            return null;

        const string tokenPattern = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"";
        var startIndex = html.IndexOf(tokenPattern, StringComparison.Ordinal);
        if (startIndex == -1)
            return null;

        startIndex += tokenPattern.Length;
        var endIndex = html.IndexOf("\"", startIndex, StringComparison.Ordinal);
        if (endIndex == -1)
            return null;

        return html.Substring(startIndex, endIndex - startIndex);
    }
}
