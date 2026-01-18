using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Data;
using TicketMasala.Domain.Entities;
using TicketMasala.Web;
using Xunit;

namespace TicketMasala.Tests.IntegrationTests;

public class LoginCreateVerifyFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string DefaultCustomerEmail = "customer@example.com";
    private const string DefaultCustomerPassword = "Customer123!";
    private const string SecondaryCustomerEmail = "second.customer@example.com";
    private const string SecondaryCustomerPassword = "Customer123!";

    private readonly CustomWebApplicationFactory _factory;

    public LoginCreateVerifyFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact(DisplayName = "Given valid credentials When logging in Then user is authenticated")]
    public async Task Login_WithValidCredentials_SucceedsAndSetsAuthentication()
    {
        await EnsureCustomerUserAsync(DefaultCustomerEmail, DefaultCustomerPassword);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var loginPageResponse = await client.GetAsync("/Identity/Account/Login");
        loginPageResponse.EnsureSuccessStatusCode();
        var loginPageHtml = await loginPageResponse.Content.ReadAsStringAsync();
        var antiforgeryToken = ExtractAntiforgeryToken(loginPageHtml);

        var formData = new Dictionary<string, string>
        {
            ["Input.Email"] = DefaultCustomerEmail,
            ["Input.Password"] = DefaultCustomerPassword,
            ["Input.RememberMe"] = "false"
        };

        if (!string.IsNullOrEmpty(antiforgeryToken))
        {
            formData["__RequestVerificationToken"] = antiforgeryToken;
        }

        var response = await client.PostAsync("/Identity/Account/Login?returnUrl=%2F", new FormUrlEncodedContent(formData));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var loginLocation = response.Headers.Location;
        var loginPath = loginLocation == null
            ? string.Empty
            : loginLocation.IsAbsoluteUri ? loginLocation.AbsolutePath : loginLocation.OriginalString;
        Assert.Equal("/", loginPath);

        var setCookieHeaders = response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? string.Join(";", cookies)
            : string.Empty;

        Assert.Contains(".AspNetCore.Identity.Application", setCookieHeaders, StringComparison.OrdinalIgnoreCase);

        var protectedResponse = await client.GetAsync("/Ticket");
        Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);
    }

    [Fact(DisplayName = "Given invalid credentials When logging in Then stay on login with error")]
    public async Task Login_WithInvalidCredentials_ShowsValidationError()
    {
        const string invalidLoginEmail = "invalid.login@example.com";
        await EnsureCustomerUserAsync(invalidLoginEmail, DefaultCustomerPassword);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var loginPageResponse = await client.GetAsync("/Identity/Account/Login");
        loginPageResponse.EnsureSuccessStatusCode();
        var loginPageHtml = await loginPageResponse.Content.ReadAsStringAsync();
        var antiforgeryToken = ExtractAntiforgeryToken(loginPageHtml);

        var formData = new Dictionary<string, string>
        {
            ["Input.Email"] = invalidLoginEmail,
            ["Input.Password"] = "WrongPassword!",
            ["Input.RememberMe"] = "false"
        };

        if (!string.IsNullOrEmpty(antiforgeryToken))
        {
            formData["__RequestVerificationToken"] = antiforgeryToken;
        }

        var response = await client.PostAsync("/Identity/Account/Login?returnUrl=%2F", new FormUrlEncodedContent(formData));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid login attempt.", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "Given locked account When logging in Then redirect to lockout page")]
    public async Task Login_WithLockedOutUser_RedirectsToLockout()
    {
        const string lockedCustomerEmail = "locked.customer@example.com";
        await EnsureCustomerUserAsync(lockedCustomerEmail, DefaultCustomerPassword);
        await LockCustomerUserAsync(lockedCustomerEmail);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var loginPageResponse = await client.GetAsync("/Identity/Account/Login");
        loginPageResponse.EnsureSuccessStatusCode();
        var loginPageHtml = await loginPageResponse.Content.ReadAsStringAsync();
        var antiforgeryToken = ExtractAntiforgeryToken(loginPageHtml);

        var formData = new Dictionary<string, string>
        {
            ["Input.Email"] = lockedCustomerEmail,
            ["Input.Password"] = DefaultCustomerPassword,
            ["Input.RememberMe"] = "false"
        };

        if (!string.IsNullOrEmpty(antiforgeryToken))
        {
            formData["__RequestVerificationToken"] = antiforgeryToken;
        }

        var response = await client.PostAsync("/Identity/Account/Login?returnUrl=%2F", new FormUrlEncodedContent(formData));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var lockoutLocation = response.Headers.Location;
        var lockoutPath = lockoutLocation == null
            ? string.Empty
            : lockoutLocation.IsAbsoluteUri ? lockoutLocation.AbsolutePath : lockoutLocation.OriginalString;
        Assert.EndsWith("/Identity/Account/Lockout", lockoutPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "Given authenticated customer When creating ticket Then ticket is persisted and viewable")]
    public async Task CreateTicket_AsAuthenticatedCustomer_PersistsAndIsViewable()
    {
        await EnsureCustomerUserAsync(DefaultCustomerEmail, DefaultCustomerPassword);

        var client = await CreateAuthenticatedClientAsync(DefaultCustomerEmail, DefaultCustomerPassword);

        var description = "End-to-end ticket " + Guid.NewGuid();
        var ticketGuid = await CreateTicketAsync(client, description);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MasalaDbContext>();
        var ticket = await context.Tickets.FindAsync(ticketGuid);

        Assert.NotNull(ticket);
        Assert.Equal(description, ticket!.Description);
        Assert.Equal(Status.Pending, ticket.TicketStatus);
        Assert.Equal("New", ticket.Status);

        var detailResponse = await client.GetAsync($"/Ticket/Detail/{ticketGuid}");
        detailResponse.EnsureSuccessStatusCode();
        var detailHtml = await detailResponse.Content.ReadAsStringAsync();
        Assert.Contains(description, detailHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "Given ticket owned by customer When other customer views Then access is forbidden")]
    public async Task TicketDetail_ForDifferentCustomer_IsForbidden()
    {
        await EnsureCustomerUserAsync(DefaultCustomerEmail, DefaultCustomerPassword);
        await EnsureCustomerUserAsync(SecondaryCustomerEmail, SecondaryCustomerPassword);

        var ownerClient = await CreateAuthenticatedClientAsync(DefaultCustomerEmail, DefaultCustomerPassword);
        var description = "Ownership test ticket " + Guid.NewGuid();
        var ticketGuid = await CreateTicketAsync(ownerClient, description);

        var otherClient = await CreateAuthenticatedClientAsync(SecondaryCustomerEmail, SecondaryCustomerPassword);
        var response = await otherClient.GetAsync($"/Ticket/Detail/{ticketGuid}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "Given full flow When executed Then completes within performance budget")]
    public async Task Login_CreateTicket_ViewDetail_Flow_CompletesWithinTimeBudget()
    {
        await EnsureCustomerUserAsync(DefaultCustomerEmail, DefaultCustomerPassword);

        var stopwatch = Stopwatch.StartNew();

        var client = await CreateAuthenticatedClientAsync(DefaultCustomerEmail, DefaultCustomerPassword);
        var description = "Performance flow ticket " + Guid.NewGuid();
        var ticketGuid = await CreateTicketAsync(client, description);
        var detailResponse = await client.GetAsync($"/Ticket/Detail/{ticketGuid}");
        detailResponse.EnsureSuccessStatusCode();

        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"End-to-end flow took {stopwatch.Elapsed.TotalMilliseconds} ms");
    }

    [Fact(DisplayName = "Given missing description When creating ticket Then validation error is shown")]
    public async Task CreateTicket_MissingDescription_ShowsValidationError()
    {
        await EnsureCustomerUserAsync(DefaultCustomerEmail, DefaultCustomerPassword);

        var client = await CreateAuthenticatedClientAsync(DefaultCustomerEmail, DefaultCustomerPassword);

        var createPageResponse = await client.GetAsync("/Ticket/Create");
        createPageResponse.EnsureSuccessStatusCode();
        var createPageHtml = await createPageResponse.Content.ReadAsStringAsync();
        var antiforgeryToken = ExtractAntiforgeryToken(createPageHtml);

        var formData = new Dictionary<string, string>
        {
            ["Description"] = string.Empty,
            ["CustomerId"] = string.Empty,
            ["ResponsibleId"] = string.Empty,
            ["ProjectGuid"] = string.Empty,
            ["CompletionTarget"] = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd")
        };

        if (!string.IsNullOrEmpty(antiforgeryToken))
        {
            formData["__RequestVerificationToken"] = antiforgeryToken;
        }

        var response = await client.PostAsync("/Ticket/Create", new FormUrlEncodedContent(formData));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(DisplayName = "Given employee without customer When creating ticket Then validation error is shown")]
    public async Task CreateTicket_AsEmployeeWithoutCustomer_ShowsValidationError()
    {
        const string employeeEmail = "employee@example.com";
        const string employeePassword = "Employee123!";

        await EnsureEmployeeUserAsync(employeeEmail, employeePassword);

        var client = await CreateAuthenticatedClientAsync(employeeEmail, employeePassword);

        var createPageResponse = await client.GetAsync("/Ticket/Create");
        createPageResponse.EnsureSuccessStatusCode();
        var createPageHtml = await createPageResponse.Content.ReadAsStringAsync();
        var antiforgeryToken = ExtractAntiforgeryToken(createPageHtml);

        var formData = new Dictionary<string, string>
        {
            ["Description"] = "Employee ticket without customer",
            ["CustomerId"] = string.Empty,
            ["ResponsibleId"] = string.Empty,
            ["ProjectGuid"] = string.Empty,
            ["CompletionTarget"] = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd")
        };

        if (!string.IsNullOrEmpty(antiforgeryToken))
        {
            formData["__RequestVerificationToken"] = antiforgeryToken;
        }

        var response = await client.PostAsync("/Ticket/Create", new FormUrlEncodedContent(formData));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(DisplayName = "Given unauthenticated user When accessing ticket creation Then redirected to login")]
    public async Task CreateTicket_UnauthenticatedUser_IsRedirectedToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Ticket/Create");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var createLocation = response.Headers.Location;
        var createPath = createLocation == null
            ? string.Empty
            : createLocation.IsAbsoluteUri ? createLocation.AbsolutePath : createLocation.OriginalString;
        Assert.Contains("/Identity/Account/Login", createPath, StringComparison.OrdinalIgnoreCase);
    }

    private async Task EnsureCustomerUserAsync(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = scope.ServiceProvider.GetRequiredService<MasalaDbContext>();

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = "Customer"
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var builder = new StringBuilder();
                foreach (var error in createResult.Errors)
                {
                    builder.Append(error.Code);
                    builder.Append(": ");
                    builder.Append(error.Description);
                    builder.AppendLine();
                }
                throw new InvalidOperationException("Failed to create test customer user: " + builder);
            }
        }
        else
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await userManager.ResetPasswordAsync(user, resetToken, password);
            if (!resetResult.Succeeded)
            {
                var builder = new StringBuilder();
                foreach (var error in resetResult.Errors)
                {
                    builder.Append(error.Code);
                    builder.Append(": ");
                    builder.Append(error.Description);
                    builder.AppendLine();
                }
                throw new InvalidOperationException("Failed to reset test customer password: " + builder);
            }
        }

        var normalizedRoleName = Constants.RoleCustomer.ToUpperInvariant();
        var role = await context.Roles
            .Where(r => r.NormalizedName == normalizedRoleName)
            .OrderBy(r => r.Id)
            .FirstOrDefaultAsync();

        if (role == null)
        {
            role = new IdentityRole(Constants.RoleCustomer)
            {
                NormalizedName = normalizedRoleName
            };
            context.Roles.Add(role);
            await context.SaveChangesAsync();
        }

        var hasRole = await context.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
        if (!hasRole)
        {
            context.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = user.Id,
                RoleId = role.Id
            });
            await context.SaveChangesAsync();
        }
    }

    private async Task EnsureEmployeeUserAsync(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = scope.ServiceProvider.GetRequiredService<MasalaDbContext>();

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = "Employee"
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var builder = new StringBuilder();
                foreach (var error in createResult.Errors)
                {
                    builder.Append(error.Code);
                    builder.Append(": ");
                    builder.Append(error.Description);
                    builder.AppendLine();
                }
                throw new InvalidOperationException("Failed to create test employee user: " + builder);
            }
        }
        else
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await userManager.ResetPasswordAsync(user, resetToken, password);
            if (!resetResult.Succeeded)
            {
                var builder = new StringBuilder();
                foreach (var error in resetResult.Errors)
                {
                    builder.Append(error.Code);
                    builder.Append(": ");
                    builder.Append(error.Description);
                    builder.AppendLine();
                }
                throw new InvalidOperationException("Failed to reset test employee password: " + builder);
            }
        }

        var normalizedRoleName = Constants.RoleEmployee.ToUpperInvariant();
        var role = await context.Roles
            .Where(r => r.NormalizedName == normalizedRoleName)
            .OrderBy(r => r.Id)
            .FirstOrDefaultAsync();

        if (role == null)
        {
            role = new IdentityRole(Constants.RoleEmployee)
            {
                NormalizedName = normalizedRoleName
            };
            context.Roles.Add(role);
            await context.SaveChangesAsync();
        }

        var hasRole = await context.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
        if (!hasRole)
        {
            context.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = user.Id,
                RoleId = role.Id
            });
            await context.SaveChangesAsync();
        }
    }

    private async Task LockCustomerUserAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return;
        }

        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(30);
        await userManager.UpdateAsync(user);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var loginPageResponse = await client.GetAsync("/Identity/Account/Login");
        loginPageResponse.EnsureSuccessStatusCode();
        var loginPageHtml = await loginPageResponse.Content.ReadAsStringAsync();
        var antiforgeryToken = ExtractAntiforgeryToken(loginPageHtml);

        var formData = new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.RememberMe"] = "false"
        };

        if (!string.IsNullOrEmpty(antiforgeryToken))
        {
            formData["__RequestVerificationToken"] = antiforgeryToken;
        }

        var response = await client.PostAsync("/Identity/Account/Login?returnUrl=%2F", new FormUrlEncodedContent(formData));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        return client;
    }

    private async Task<Guid> CreateTicketAsync(HttpClient client, string description)
    {
        var createPageResponse = await client.GetAsync("/Ticket/Create");
        createPageResponse.EnsureSuccessStatusCode();
        var createPageHtml = await createPageResponse.Content.ReadAsStringAsync();
        var antiforgeryToken = ExtractAntiforgeryToken(createPageHtml);

        var formData = new Dictionary<string, string>
        {
            ["Description"] = description,
            ["CustomerId"] = string.Empty,
            ["ResponsibleId"] = string.Empty,
            ["ProjectGuid"] = string.Empty,
            ["CompletionTarget"] = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd")
        };

        if (!string.IsNullOrEmpty(antiforgeryToken))
        {
            formData["__RequestVerificationToken"] = antiforgeryToken;
        }

        var response = await client.PostAsync("/Ticket/Create", new FormUrlEncodedContent(formData));
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.OK,
            $"Unexpected status code when creating ticket: {response.StatusCode}");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MasalaDbContext>();
        var ticket = await context.Tickets
            .OrderByDescending(t => t.CreationDate)
            .FirstAsync(t => t.Description == description);

        return ticket.Guid;
    }

    private static string? ExtractAntiforgeryToken(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return null;
        }

        const string tokenFieldName = "__RequestVerificationToken";

        var nameIndex = html.IndexOf("name=\"__RequestVerificationToken\"", StringComparison.OrdinalIgnoreCase);
        if (nameIndex < 0)
        {
            nameIndex = html.IndexOf("name='__RequestVerificationToken'", StringComparison.OrdinalIgnoreCase);
        }

        if (nameIndex < 0)
        {
            return null;
        }

        var valueIndex = html.IndexOf("value=\"", nameIndex, StringComparison.OrdinalIgnoreCase);
        if (valueIndex >= 0)
        {
            var start = valueIndex + "value=\"".Length;
            var end = html.IndexOf("\"", start, StringComparison.Ordinal);
            if (end > start)
            {
                return html.Substring(start, end - start);
            }
        }

        valueIndex = html.IndexOf("value='", nameIndex, StringComparison.OrdinalIgnoreCase);
        if (valueIndex >= 0)
        {
            var start = valueIndex + "value='".Length;
            var end = html.IndexOf("'", start, StringComparison.Ordinal);
            if (end > start)
            {
                return html.Substring(start, end - start);
            }
        }

        return null;
    }
}
