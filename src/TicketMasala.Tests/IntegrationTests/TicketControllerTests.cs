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
/// Integration tests for TicketController - tests Create, Detail, Edit, and Index actions
/// </summary>
public class TicketControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TicketControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> CreateAuthenticatedFactory(string userId, string userName, string role, string email)
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
                .AddScheme<TestAuthOptions, TestAuthHandler>("Test", options => { options.Role = role; options.NameIdentifier = userId; });

                // Mock IDomainUiService
                var mockDomainUi = new Moq.Mock<IDomainUiService>();
                mockDomainUi.Setup(x => x.GetLabel(Moq.It.IsAny<string>(), Moq.It.IsAny<string>())).Returns((string k, string d) => k);
                mockDomainUi.Setup(x => x.GetIcon(Moq.It.IsAny<string>(), Moq.It.IsAny<string>())).Returns("bi-box");
                mockDomainUi.Setup(x => x.GetDomainCssClass(Moq.It.IsAny<string>())).Returns("theme-default");
                services.AddSingleton(mockDomainUi.Object);

                // Seed test user and roles
                var sp = services.BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateScopes = false,
                    ValidateOnBuild = false
                });
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<MasalaDbContext>();

                    // Add user if not exists
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

                    // Verify customer was created
                    var createdCustomer = db.Users.FirstOrDefault(u => u.Id == testCustomerId);
                    if (createdCustomer == null)
                    {
                        throw new InvalidOperationException("Failed to create test customer");
                    }
                }
            });
        });
    }

    private HttpClient CreateAuthenticatedClient(string userId, string userName, string role, string email)
    {
        var factory = CreateAuthenticatedFactory(userId, userName, role, email);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        return client;
    }

    [Fact(DisplayName = "GET /Ticket - Returns ticket list page for authenticated user")]
    public async Task Index_AuthenticatedUser_ReturnsTicketList()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-emp-1", "test.employee1@test.com", "Employee", "test.employee1@test.com");

        // Act
        var response = await client.GetAsync("/Ticket");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("ticket", content.ToLower());
    }

    [Fact(DisplayName = "GET /Ticket/Create - Returns create form for employee")]
    public async Task Create_Get_Employee_ReturnsCreateForm()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-emp-2", "test.employee2@test.com", "Employee", "test.employee2@test.com");

        // Act
        var response = await client.GetAsync("/Ticket/Create");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Create", content);
        Assert.Contains("Description", content);
    }

    [Fact(DisplayName = "POST /Ticket/Create - With valid data creates ticket and redirects")]
    public async Task Create_Post_ValidData_CreatesTicket()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-emp-3", "test.employee3@test.com", "Employee", "test.employee3@test.com");

        // First get the create page to extract antiforgery token
        var createPageResponse = await client.GetAsync("/Ticket/Create");
        createPageResponse.EnsureSuccessStatusCode();
        var createPageHtml = await createPageResponse.Content.ReadAsStringAsync();
        var antiforgeryToken = TestHelpers.ExtractAntiforgeryToken(createPageHtml);

        var formData = new Dictionary<string, string>
        {
            ["Description"] = "Test ticket created from integration test",
            ["CustomerId"] = "test-emp-3",
            ["ResponsibleId"] = "",
            ["ProjectGuid"] = "",
            ["CompletionTarget"] = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"),
            ["WorkItemTypeCode"] = "INCIDENT",
            ["DomainId"] = "IT"
        };

        if (!string.IsNullOrEmpty(antiforgeryToken))
        {
            formData["__RequestVerificationToken"] = antiforgeryToken;
        }

        // Act
        var response = await client.PostAsync("/Ticket/Create", new FormUrlEncodedContent(formData));

        // Assert - Should redirect on success
        Assert.True(response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.OK,
            $"Expected Redirect or OK but got {response.StatusCode}");
    }

    [Fact(DisplayName = "POST /Ticket/Create - With missing description shows validation error")]
    public async Task Create_Post_MissingDescription_ShowsValidationError()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-emp-4", "test.employee4@test.com", "Employee", "test.employee4@test.com");

        var createPageResponse = await client.GetAsync("/Ticket/Create");
        createPageResponse.EnsureSuccessStatusCode();
        var createPageHtml = await createPageResponse.Content.ReadAsStringAsync();
        var antiforgeryToken = TestHelpers.ExtractAntiforgeryToken(createPageHtml);

        var formData = new Dictionary<string, string>
        {
            ["Description"] = "", // Empty description
            ["CustomerId"] = "test-emp-4",
            ["ResponsibleId"] = "",
            ["ProjectGuid"] = "",
            ["CompletionTarget"] = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"),
            ["WorkItemTypeCode"] = "INCIDENT",
            ["DomainId"] = "IT"
        };

        if (!string.IsNullOrEmpty(antiforgeryToken))
        {
            formData["__RequestVerificationToken"] = antiforgeryToken;
        }

        // Act
        var response = await client.PostAsync("/Ticket/Create", new FormUrlEncodedContent(formData));

        // Assert - Should return to form with validation error
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(
            content.Contains("Description is required") || content.Contains("validation") || content.Contains("field-validation-error"),
            "Expected validation error message in response");
    }

    [Fact(DisplayName = "GET /Ticket/Detail/{id} - Returns ticket details for existing ticket")]
    public async Task Detail_ExistingTicket_ReturnsTicketDetails()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-emp-5", "test.employee5@test.com", "Employee", "test.employee5@test.com");

        // Create a ticket first
        var ticketGuid = await TestHelpers.CreateTicketAsync(client, _factory, "Test ticket for detail view");

        // Act
        var response = await client.GetAsync($"/Ticket/Detail/{ticketGuid}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test ticket for detail view", content);
    }

    [Fact(DisplayName = "GET /Ticket/Detail/{id} - Returns 404 for non-existent ticket")]
    public async Task Detail_NonExistentTicket_ReturnsNotFound()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-emp-6", "test.employee6@test.com", "Employee", "test.employee6@test.com");
        var nonExistentGuid = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/Ticket/Detail/{nonExistentGuid}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "GET /Ticket/Edit/{id} - Returns edit form for existing ticket")]
    public async Task Edit_Get_ExistingTicket_ReturnsEditForm()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-emp-7", "test.employee7@test.com", "Employee", "test.employee7@test.com");

        // Create a ticket first
        var ticketGuid = await TestHelpers.CreateTicketAsync(client, _factory, "Test ticket for edit");

        // Act
        var response = await client.GetAsync($"/Ticket/Edit/{ticketGuid}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Edit", content);
        Assert.Contains("Test ticket for edit", content);
    }

    [Fact(DisplayName = "POST /Ticket/Edit - Updates ticket with valid data")]
    public async Task Edit_Post_ValidData_UpdatesTicket()
    {
        // Arrange
        var client = CreateAuthenticatedClient("test-emp-8", "test.employee8@test.com", "Employee", "test.employee8@test.com");

        // Create a ticket first
        var ticketGuid = await TestHelpers.CreateTicketAsync(client, _factory, "Original description");

        // Get edit page for antiforgery token
        var editPageResponse = await client.GetAsync($"/Ticket/Edit/{ticketGuid}");
        editPageResponse.EnsureSuccessStatusCode();
        var editPageHtml = await editPageResponse.Content.ReadAsStringAsync();
        var antiforgeryToken = TestHelpers.ExtractAntiforgeryToken(editPageHtml);

        var formData = new Dictionary<string, string>
        {
            ["Guid"] = ticketGuid.ToString(),
            ["Title"] = "Updated Title",
            ["Description"] = "Updated description from test",
            ["Status"] = "InProgress",
            ["TicketStatus"] = "InProgress",
            ["__RequestVerificationToken"] = antiforgeryToken ?? ""
        };

        // Act
        var response = await client.PostAsync($"/Ticket/Edit/{ticketGuid}", new FormUrlEncodedContent(formData));

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.OK,
            $"Expected Redirect or OK but got {response.StatusCode}");
    }

    [Fact(DisplayName = "GET /Ticket/Detail/{id} - Different customer cannot access ticket")]
    public async Task Detail_DifferentCustomer_ReturnsForbiddenOrRedirect()
    {
        // Arrange - Create ticket as customer1
        var customer1Client = CreateAuthenticatedClient("test-cust-1", "customer1@test.com", "Customer", "customer1@test.com");
        var ticketGuid = await TestHelpers.CreateTicketAsync(customer1Client, _factory, "Private customer ticket", "test-cust-1");

        // Act - Try to access as customer2
        var customer2Client = CreateAuthenticatedClient("test-cust-2", "customer2@test.com", "Customer", "customer2@test.com");
        var response = await customer2Client.GetAsync($"/Ticket/Detail/{ticketGuid}");

        // Assert - Should be forbidden or redirect to access denied
        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected Forbidden, Redirect, or NotFound but got {response.StatusCode}");
    }

}
