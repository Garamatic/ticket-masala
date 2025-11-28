using IT_Project2526;
using IT_Project2526.Data;
using IT_Project2526.Managers;
using IT_Project2526.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebOptimizer;

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
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = false;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;

    // SignIn settings
    options.SignIn.RequireConfirmedEmail = false;
})
    .AddEntityFrameworkStores<ITProjectDB>()
    .AddDefaultTokenProviders()
    .AddDefaultUI(); //for identity pages

// Register Managers (if needed in future)
builder.Services.AddScoped<ApplicationUserManager>();

// Register DbSeeder
builder.Services.AddScoped<DbSeeder>();

// Add Memory Cache
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

// Configure WebOptimizer for CSS/JS bundling and minification
builder.Services.AddWebOptimizer(pipeline =>
{
    // Bundle and minify CSS files
    pipeline.AddCssBundle("/css/bundle.css",
        "lib/bootstrap/dist/css/bootstrap.min.css",
        "css/design-system.css",
        "css/site.css")
        .MinifyCss();
    
    // Bundle and minify JavaScript files
    pipeline.AddJavaScriptBundle("/js/bundle.js",
        "lib/jquery/dist/jquery.min.js",
        "lib/bootstrap/dist/js/bootstrap.bundle.min.js",
        "js/site.js",
        "js/toast.js")
        .MinifyJavaScript();
});

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
builder.Services.AddRazorPages(); // keep Razor Pages for Identity UI

var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("==================================================");
        logger.LogInformation("Checking if database seeding is needed...");
        logger.LogInformation("==================================================");
        
        var seeder = services.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync();
        
        logger.LogInformation("==================================================");
        logger.LogInformation("Database check completed");
        logger.LogInformation("==================================================");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "==================================================");
        logger.LogError(ex, "CRITICAL: An error occurred while seeding the database.");
        logger.LogError(ex, "==================================================");
        logger.LogError("You may need to manually create test users or check your database connection");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Use WebOptimizer middleware
app.UseWebOptimizer();

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
