using TicketMasala.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TicketMasala.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Controllers;
    // Seed controller only accessible in Development environment
    public class SeedController : Controller
    {
        private readonly DbSeeder _seeder;
        private readonly ILogger<SeedController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MasalaDbContext _context;

        public SeedController(
            DbSeeder seeder, 
            ILogger<SeedController> logger, 
            IWebHostEnvironment env,
            UserManager<ApplicationUser> userManager,
            MasalaDbContext context)
        {
            _seeder = seeder;
            _logger = logger;
            _env = env;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Only allow in development environment
            if (!_env.IsDevelopment())
            {
                return NotFound();
            }

            try
            {
                await _seeder.SeedAsync();
                return Content("Database seeded successfully! Check the logs for details. You can now close this page and try logging in.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding database");
                return Content($"Error seeding database: {ex.Message}\n\nInner Exception: {ex.InnerException?.Message}\n\nStack Trace: {ex.StackTrace}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> DeleteAllUsers()
        {
            if (!_env.IsDevelopment())
            {
                return NotFound();
            }

            try
            {
                var users = await _userManager.Users.ToListAsync();
                var count = users.Count;

                foreach (var user in users)
                {
                    await _userManager.DeleteAsync(user);
                }

                return Content($"Deleted {count} users successfully! Now go to /Seed/Index to create test accounts.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting users");
                return Content($"Error: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetDatabase()
        {
            if (!_env.IsDevelopment())
            {
                return NotFound();
            }

            try
            {
                // Delete all users
                var users = await _userManager.Users.ToListAsync();
                foreach (var user in users)
                {
                    await _userManager.DeleteAsync(user);
                }

                // Delete all projects and tickets
                _context.Tickets.RemoveRange(_context.Tickets);
                _context.Projects.RemoveRange(_context.Projects);
                await _context.SaveChangesAsync();

                // Seed new data
                await _seeder.SeedAsync();

                return Content("Database reset and seeded successfully! You can now login with the test accounts.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting database");
                return Content($"Error: {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestAccounts()
        {
            if (!_env.IsDevelopment())
            {
                return NotFound();
            }

            var userCount = await _context.Users.CountAsync();

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Test Accounts</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; background: #f5f5f5; }}
        .container {{ background: white; padding: 30px; border-radius: 8px; max-width: 800px; margin: 0 auto; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        h1 {{ color: #333; border-bottom: 3px solid #007bff; padding-bottom: 10px; }}
        h2 {{ color: #666; margin-top: 30px; }}
        .account {{ background: #f8f9fa; padding: 15px; margin: 10px 0; border-left: 4px solid #007bff; border-radius: 4px; }}
        .account strong {{ color: #007bff; }}
        .password {{ color: #28a745; font-family: monospace; }}
        .btn {{ display: inline-block; padding: 10px 20px; background: #007bff; color: white; text-decoration: none; border-radius: 4px; margin: 10px 5px 10px 0; }}
        .btn:hover {{ background: #0056b3; }}
        .btn-danger {{ background: #dc3545; }}
        .btn-danger:hover {{ background: #c82333; }}
        .btn-success {{ background: #28a745; }}
        .btn-success:hover {{ background: #218838; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 4px; }}
        .info {{ background: #d1ecf1; border-left: 4px solid #17a2b8; padding: 15px; margin: 20px 0; border-radius: 4px; }}
        .danger {{ background: #f8d7da; border-left: 4px solid #dc3545; padding: 15px; margin: 20px 0; border-radius: 4px; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>?? Test Accounts</h1>
        
        <div class='warning'>
            <strong>?? Development Mode Only</strong><br>
            These test accounts are for development purposes only.
        </div>

        <div class='info'>
            <strong>?? Current Status:</strong><br>
            Database contains <strong>{userCount}</strong> user(s).<br>
            {(userCount > 0 ? "?? Existing users found. You may need to reset the database if test account passwords don't work." : "? Database is empty and ready for seeding!")}
        </div>

        <h2>?? Actions</h2>
        {(userCount > 0 ? 
            "<a href='/Seed/ResetDatabase' class='btn btn-danger' onclick='return confirm(\"Are you sure? This will delete ALL data and create fresh test accounts.\")'>?? Reset Database & Seed</a>" :
            "<a href='/Seed/Index' class='btn btn-success'>?? Run Database Seeder</a>"
        )}
        <a href='/Identity/Account/Login' class='btn'>?? Go to Login Page</a>

        <h2>?? Admin Accounts</h2>
        <div class='account'>
            <strong>Email:</strong> admin@ticketmasala.com<br>
            <strong>Password:</strong> <span class='password'>Admin123!</span>
        </div>

        <h2>????? Employee Accounts</h2>
        <div class='account'>
            <strong>Project Manager - Mike Johnson</strong><br>
            <strong>Email:</strong> mike.pm@ticketmasala.com<br>
            <strong>Password:</strong> <span class='password'>Employee123!</span>
        </div>
        <div class='account'>
            <strong>Support - David Martinez</strong><br>
            <strong>Email:</strong> david.support@ticketmasala.com<br>
            <strong>Password:</strong> <span class='password'>Employee123!</span>
        </div>

        <h2>????? Customer Accounts</h2>
        <div class='account'>
            <strong>Alice Smith</strong><br>
            <strong>Email:</strong> alice.customer@example.com<br>
            <strong>Password:</strong> <span class='password'>Customer123!</span>
        </div>

        <h2>?? Password Requirements</h2>
        <ul>
            <li>At least 8 characters long</li>
            <li>At least 1 digit</li>
            <li>At least 1 uppercase letter</li>
        </ul>

        <h2>?? Troubleshooting</h2>
        <ol>
            <li>If database has existing users, click ""Reset Database & Seed"" button</li>
            <li>If database is empty, click ""Run Database Seeder"" button</li>
            <li>Wait for success message</li>
            <li>Go to login page and try an account</li>
            <li>If still not working, check the application logs</li>
        </ol>
    </div>
</body>
</html>";

            return Content(html, "text/html");
        }
}
