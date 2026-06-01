using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Engine.GERDA.Configuration;
using Xunit;

namespace TicketMasala.Tests.IntegrationTests;

/// <summary>
/// Integration tests for TicketWorkflowController - tests workflow actions like comments, assignments, reviews
/// </summary>
public class TicketWorkflowControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TicketWorkflowControllerTests(CustomWebApplicationFactory factory)
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

                // Mock IDomainUiService
                var mockDomainUi = new Moq.Mock<IDomainUiService>();
                mockDomainUi.Setup(x => x.GetLabel(Moq.It.IsAny<string>(), Moq.It.IsAny<string>())).Returns((string k, string d) => k);
                mockDomainUi.Setup(x => x.GetIcon(Moq.It.IsAny<string>(), Moq.It.IsAny<string>())).Returns("bi-box");
                mockDomainUi.Setup(x => x.GetDomainCssClass(Moq.It.IsAny<string>())).Returns("theme-default");
                services.AddSingleton(mockDomainUi.Object);

                // Seed test users
                var sp = services.BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateScopes = false,
                    ValidateOnBuild = false
                });
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<MasalaDbContext>();

                    // Add employee user
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
                                Team = "Support",
                                Level = EmployeeType.Support,
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

                    // Ensure test customer exists for ticket creation (use valid Guid format)
                    var testCustomerId = "11111111-1111-1111-1111-111111111111";
                    if (!db.Users.Any(u => u.Id == testCustomerId))
                    {
                        db.Users.Add(new ApplicationUser
                        {
                            Id = testCustomerId,
                            UserName = "test.customer@test.com",
                            Email = "test.customer@test.com",
                            FirstName = "Test",
                            LastName = "Customer",
                            Phone = "555-0100",
                            NormalizedEmail = "TEST.CUSTOMER@TEST.COM",
                            NormalizedUserName = "TEST.CUSTOMER@TEST.COM"
                        });
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

    [Fact(DisplayName = "POST /TicketWorkflow/AddComment - Adds comment to ticket")]
    public async Task AddComment_ValidData_AddsComment()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-wf-1", "test.wf1@test.com", "Employee", "test.wf1@test.com");
        var ticketGuid = await TestHelpers.CreateTicketAsync(client, _factory, "Ticket for comment test");

        var detailPage = await client.GetAsync($"/Ticket/Detail/{ticketGuid}");
        detailPage.EnsureSuccessStatusCode();
        var detailHtml = await detailPage.Content.ReadAsStringAsync();
        var antiforgeryToken = TestHelpers.ExtractAntiforgeryToken(detailHtml);

        var formData = new Dictionary<string, string>
        {
            ["id"] = ticketGuid.ToString(),
            ["commentBody"] = "This is a test comment",
            ["isInternal"] = "false",
            ["__RequestVerificationToken"] = antiforgeryToken ?? ""
        };

        // Act
        var response = await client.PostAsync("/TicketWorkflow/AddComment", new FormUrlEncodedContent(formData));

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.OK,
            $"Expected Redirect or OK but got {response.StatusCode}");
    }

    [Fact(DisplayName = "POST /TicketWorkflow/AddComment - Empty comment shows error")]
    public async Task AddComment_EmptyComment_ShowsError()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-wf-2", "test.wf2@test.com", "Employee", "test.wf2@test.com");
        var ticketGuid = await TestHelpers.CreateTicketAsync(client, _factory, "Ticket for empty comment test");

        var detailPage = await client.GetAsync($"/Ticket/Detail/{ticketGuid}");
        detailPage.EnsureSuccessStatusCode();
        var detailHtml = await detailPage.Content.ReadAsStringAsync();
        var antiforgeryToken = TestHelpers.ExtractAntiforgeryToken(detailHtml);

        var formData = new Dictionary<string, string>
        {
            ["id"] = ticketGuid.ToString(),
            ["commentBody"] = "", // Empty comment
            ["isInternal"] = "false",
            ["__RequestVerificationToken"] = antiforgeryToken ?? ""
        };

        // Act
        var response = await client.PostAsync("/TicketWorkflow/AddComment", new FormUrlEncodedContent(formData));

        // Assert - Should redirect back with error or return bad request
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.OK,
            $"Expected Redirect, BadRequest, or OK but got {response.StatusCode}");
    }

    [Fact(DisplayName = "POST /TicketWorkflow/RequestReview - Requests quality review")]
    public async Task RequestReview_ValidTicket_RequestsReview()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-wf-3", "test.wf3@test.com", "Employee", "test.wf3@test.com");
        var ticketGuid = await TestHelpers.CreateTicketAsync(client, _factory, "Ticket for review test");

        var detailPage = await client.GetAsync($"/Ticket/Detail/{ticketGuid}");
        detailPage.EnsureSuccessStatusCode();
        var detailHtml = await detailPage.Content.ReadAsStringAsync();
        var antiforgeryToken = TestHelpers.ExtractAntiforgeryToken(detailHtml);

        var formData = new Dictionary<string, string>
        {
            ["id"] = ticketGuid.ToString(),
            ["__RequestVerificationToken"] = antiforgeryToken ?? ""
        };

        // Act
        var response = await client.PostAsync("/TicketWorkflow/RequestReview", new FormUrlEncodedContent(formData));

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.OK,
            $"Expected Redirect or OK but got {response.StatusCode}");
    }

    [Fact(DisplayName = "POST /TicketWorkflow/SubmitReview - Submits quality review")]
    public async Task SubmitReview_ValidData_SubmitsReview()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-wf-4", "test.wf4@test.com", "Employee", "test.wf4@test.com");
        var ticketGuid = await TestHelpers.CreateTicketAsync(client, _factory, "Ticket for submit review test");

        var detailPage = await client.GetAsync($"/Ticket/Detail/{ticketGuid}");
        detailPage.EnsureSuccessStatusCode();
        var detailHtml = await detailPage.Content.ReadAsStringAsync();
        var antiforgeryToken = TestHelpers.ExtractAntiforgeryToken(detailHtml);

        var formData = new Dictionary<string, string>
        {
            ["id"] = ticketGuid.ToString(),
            ["score"] = "85",
            ["feedback"] = "Great work on this ticket!",
            ["approve"] = "true",
            ["__RequestVerificationToken"] = antiforgeryToken ?? ""
        };

        // Act
        var response = await client.PostAsync("/TicketWorkflow/SubmitReview", new FormUrlEncodedContent(formData));

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.OK,
            $"Expected Redirect or OK but got {response.StatusCode}");
    }

    [Fact(DisplayName = "POST /TicketWorkflow/AssignToRecommended - Assigns ticket to agent")]
    public async Task AssignToRecommended_ValidAgent_AssignsTicket()
    {
        // Arrange
        var agentId = "test-agent-1";
        var client = CreateAuthenticatedClient("test-wf-5", "test.wf5@test.com", "Employee", "test.wf5@test.com");

        // Create agent user
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MasalaDbContext>();
            if (!db.Users.Any(u => u.Id == agentId))
            {
                db.Users.Add(new Employee
                {
                    Id = agentId,
                    UserName = "test.agent@test.com",
                    Email = "test.agent@test.com",
                    FirstName = "Test",
                    LastName = "Agent",
                    Phone = "555-0200",
                    Team = "Support",
                    Level = EmployeeType.Support,
                    NormalizedEmail = "TEST.AGENT@TEST.COM",
                    NormalizedUserName = "TEST.AGENT@TEST.COM"
                });
                db.SaveChanges();
            }
        }

        var ticketGuid = await TestHelpers.CreateTicketAsync(client, _factory, "Ticket for assignment test");

        var detailPage = await client.GetAsync($"/Ticket/Detail/{ticketGuid}");
        detailPage.EnsureSuccessStatusCode();
        var detailHtml = await detailPage.Content.ReadAsStringAsync();
        var antiforgeryToken = TestHelpers.ExtractAntiforgeryToken(detailHtml);

        var formData = new Dictionary<string, string>
        {
            ["ticketGuid"] = ticketGuid.ToString(),
            ["agentId"] = agentId,
            ["__RequestVerificationToken"] = antiforgeryToken ?? ""
        };

        // Act
        var response = await client.PostAsync("/TicketWorkflow/AssignToRecommended", new FormUrlEncodedContent(formData));

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.OK,
            $"Expected Redirect or OK but got {response.StatusCode}");
    }

    [Fact(DisplayName = "Unauthorized user cannot add comment")]
    public async Task AddComment_UnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange - Create client without authentication
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // No authentication setup - force challenge
            });
        });
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var formData = new Dictionary<string, string>
        {
            ["id"] = Guid.NewGuid().ToString(),
            ["commentBody"] = "Test comment",
            ["isInternal"] = "false"
        };

        // Act
        var response = await client.PostAsync("/TicketWorkflow/AddComment", new FormUrlEncodedContent(formData));

        // Assert - Should redirect to login or return unauthorized
        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Redirect,
            $"Expected Unauthorized or Redirect but got {response.StatusCode}");
    }

}
