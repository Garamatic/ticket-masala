using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;
using Xunit;

namespace TicketMasala.Tests.IntegrationTests;

public class TestAuthOptions : AuthenticationSchemeOptions
{
    public string Role { get; set; } = "Customer";
    public string NameIdentifier { get; set; } = "test-customer-id";
    public string Name { get; set; } = "Test Customer";
}

public class TestAuthHandler : AuthenticationHandler<TestAuthOptions>
{
    public TestAuthHandler(IOptionsMonitor<TestAuthOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] {
            new Claim(ClaimTypes.Name, Options.Name),
            new Claim(ClaimTypes.NameIdentifier, Options.NameIdentifier),
            new Claim(ClaimTypes.Role, Options.Role)
        };
        // Use IdentityConstants.ApplicationScheme so SignInManager.IsSignedIn returns true
        var identity = new ClaimsIdentity(claims, "Identity.Application"); // Hardcoding string to avoid reference hell if package isn't directly compatible in test project

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class AuthenticatedRouteTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthenticatedRouteTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TicketRoute_ReturnsSuccess_WhenAuthenticated()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<TestAuthOptions, TestAuthHandler>("Test", options => { });

                // Mock IDomainUiService (consumed by _Layout.cshtml)
                var mockDomainUi = new Moq.Mock<TicketMasala.Web.Engine.GERDA.Configuration.IDomainUiService>();
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
                    if (!db.Users.Any(u => u.Id == "test-customer-id"))
                    {
                        db.Users.Add(new ApplicationUser
                        {
                            Id = "test-customer-id",
                            UserName = "test.customer",
                            Email = "test@example.com",
                            FirstName = "Test",
                            LastName = "Customer",
                            PhoneNumber = "555-0100",
                            Phone = "555-0100"
                        });
                        db.SaveChanges();
                    }
                }
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        // Act
        var response = await client.GetAsync("/Ticket");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Customer", content); // User name usually appears in nav
    }
}
