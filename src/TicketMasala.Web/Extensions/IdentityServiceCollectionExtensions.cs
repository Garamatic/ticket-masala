using Microsoft.AspNetCore.Identity;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Data;

namespace TicketMasala.Web.Extensions;

/// <summary>
/// Extension methods for configuring ASP.NET Core Identity.
/// </summary>
public static class IdentityServiceCollectionExtensions
{
    /// <summary>
    /// Adds and configures ASP.NET Core Identity with secure defaults.
    /// </summary>
    public static IServiceCollection AddMasalaIdentity(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequiredUniqueChars = 2;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;

            // SignIn settings
            options.SignIn.RequireConfirmedEmail = false;
        })
            .AddEntityFrameworkStores<MasalaDbContext>()
            .AddDefaultTokenProviders()
            .AddDefaultUI();

        return services;
    }

    // NOTE: Cookie configuration moved to WebApplicationBuilderExtensions.AddMasalaCore()
    // to have access to IWebHostEnvironment for conditional SecurePolicy.
    // This extension is kept for backward compatibility if other code references it,
    // but new code should configure cookies in AddMasalaCore.
}
