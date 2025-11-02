using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using IT_Project2526.Models;
using IT_Project2526;
using Microsoft.AspNetCore.Authorization;
using IT_Project2526.Managers;

namespace IT_Project2526
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<ITProjectDB>(options =>
                options.UseSqlServer(connectionString, sqlServerOptions =>
                {
                    sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                }));

            //Identity configuration
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;              // Require at least one digit
                options.Password.RequiredLength = 8;              // Minimum length
                options.Password.RequireNonAlphanumeric = false;  // Require symbols like !@#
                options.Password.RequireUppercase = true;        // Require uppercase letters
                options.Password.RequireLowercase = false;        // Require lowercase letters
                options.Password.RequiredUniqueChars = 1;        // Minimum unique characters

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;

                // SignIn settings
                options.SignIn.RequireConfirmedEmail = false; // set true if we want email confirmation
            })
                .AddEntityFrameworkStores<ITProjectDB>()
                .AddDefaultTokenProviders()
                .AddDefaultUI(); //for identity pages

            //register applicationusermanager service so it's available in the controller
            builder.Services.AddScoped<ApplicationUserManager>();

            //Authorization
            builder.Services.AddAuthorization(options =>
            {
                if (!builder.Environment.IsDevelopment())
                {
                    options.FallbackPolicy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                }
            });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                // Cookie settings
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(120);

                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                options.SlidingExpiration = true;
            });


            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapRazorPages(); // Identity pages like /Identity/Login

            app.Run();
        }
    }
}
