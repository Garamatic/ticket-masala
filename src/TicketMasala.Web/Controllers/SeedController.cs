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
        public async Task<IActionResult> RunSeed()
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
        public async Task<IActionResult> Index()
        {
            if (!_env.IsDevelopment())
            {
                return NotFound();
            }

            var userCount = await _context.Users.CountAsync();
            return View(userCount);
        }
}
