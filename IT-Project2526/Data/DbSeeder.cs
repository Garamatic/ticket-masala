using IT_Project2526.Models;
using IT_Project2526.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Project2526.Data
{
    public class DbSeeder
    {
        private readonly ITProjectDB _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DbSeeder> _logger;
        private readonly IWebHostEnvironment _environment;

        public DbSeeder(ITProjectDB context, UserManager<ApplicationUser> userManager, ILogger<DbSeeder> logger, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _environment = environment;
        }

        private async Task<bool> CheckTablesExistAsync()
        {
            try
            {
                // Simple check: try to count users - if table doesn't exist, this will throw
                _ = await _context.Users.CountAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task SeedAsync()
        {
            try
            {
                _logger.LogInformation("========== DATABASE SEEDING STARTED ==========");
                
                // Ensure database is created
                _logger.LogInformation("Ensuring database exists...");
                
                // Use EnsureCreated for SQLite in production (Fly), Migrate for SQL Server in dev
                if (_environment.IsProduction())
                {
                    _logger.LogInformation("Production mode: Using SQLite with EnsureCreated");
                    
                    // First, always try EnsureCreated - it's idempotent (does nothing if DB exists)
                    var created = await _context.Database.EnsureCreatedAsync();
                    _logger.LogInformation("EnsureCreatedAsync result: {Created}", created);
                    
                    // Verify tables exist
                    var tablesExist = await CheckTablesExistAsync();
                    if (!tablesExist)
                    {
                        _logger.LogError("CRITICAL: Tables still don't exist after EnsureCreatedAsync!");
                        throw new Exception("Failed to create database tables");
                    }
                    
                    _logger.LogInformation("SQLite database tables verified");
                }
                else
                {
                    // Apply migrations for SQL Server in development
                    await _context.Database.MigrateAsync();
                    _logger.LogInformation("Database migrations applied successfully");
                }

                // Check if we already have users
                var userCount = await _context.Users.CountAsync();
                _logger.LogInformation("Current user count in database: {UserCount}", userCount);
                
                if (userCount > 0)
                {
                    _logger.LogWarning("Database already contains {UserCount} users. Skipping seed.", userCount);
                    _logger.LogInformation("If you want to re-seed, please delete all users first or drop the database");
                    return;
                }

                _logger.LogInformation("Database is empty. Starting to create test accounts...");

                // Create Admin Users
                _logger.LogInformation("Creating admin users...");
                await CreateAdminUsers();

                // Create Employee Users
                _logger.LogInformation("Creating employee users...");
                await CreateEmployeeUsers();

                // Create Customer Users
                _logger.LogInformation("Creating customer users...");
                await CreateCustomerUsers();

                // Create Sample Projects
                _logger.LogInformation("Creating sample projects...");
                await CreateSampleProjects();

                // Create Sample Tickets
                _logger.LogInformation("Creating sample tickets...");
                await CreateSampleTickets();

                _logger.LogInformation("========== DATABASE SEEDING COMPLETED SUCCESSFULLY! ==========");
                _logger.LogInformation("You can now login with any of the test accounts");
                _logger.LogInformation("Admin: admin@ticketmasala.com / Admin123!");
                _logger.LogInformation("Employee: mike.pm@ticketmasala.com / Employee123!");
                _logger.LogInformation("Customer: alice.customer@example.com / Customer123!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "========== ERROR DURING DATABASE SEEDING ==========");
                _logger.LogError("Error message: {Message}", ex.Message);
                _logger.LogError("Inner exception: {InnerException}", ex.InnerException?.Message);
                throw;
            }
        }

        private async Task CreateAdminUsers()
        {
            var admins = new[]
            {
                new Employee
                {
                    UserName = "admin@ticketmasala.com",
                    Email = "admin@ticketmasala.com",
                    EmailConfirmed = true,
                    FirstName = "John",
                    LastName = "Administrator",
                    Phone = "+1-555-0100",
                    Team = "Management",
                    Level = EmployeeType.Admin
                },
                new Employee
                {
                    UserName = "sarah.admin@ticketmasala.com",
                    Email = "sarah.admin@ticketmasala.com",
                    EmailConfirmed = true,
                    FirstName = "Sarah",
                    LastName = "Wilson",
                    Phone = "+1-555-0101",
                    Team = "Management",
                    Level = EmployeeType.CEO
                }
            };

            foreach (var admin in admins)
            {
                var result = await _userManager.CreateAsync(admin, "Admin123!");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(admin, Constants.RoleAdmin);
                    _logger.LogInformation("Created admin user: {Email}", admin.Email);
                }
                else
                {
                    _logger.LogError("Failed to create admin user {Email}: {Errors}", 
                        admin.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        private async Task CreateEmployeeUsers()
        {
            var employees = new[]
            {
                new Employee
                {
                    UserName = "mike.pm@ticketmasala.com",
                    Email = "mike.pm@ticketmasala.com",
                    EmailConfirmed = true,
                    FirstName = "Mike",
                    LastName = "Johnson",
                    Phone = "+1-555-0200",
                    Team = "Project Management",
                    Level = EmployeeType.ProjectManager
                },
                new Employee
                {
                    UserName = "lisa.pm@ticketmasala.com",
                    Email = "lisa.pm@ticketmasala.com",
                    EmailConfirmed = true,
                    FirstName = "Lisa",
                    LastName = "Chen",
                    Phone = "+1-555-0201",
                    Team = "Project Management",
                    Level = EmployeeType.ProjectManager
                },
                new Employee
                {
                    UserName = "david.support@ticketmasala.com",
                    Email = "david.support@ticketmasala.com",
                    EmailConfirmed = true,
                    FirstName = "David",
                    LastName = "Martinez",
                    Phone = "+1-555-0300",
                    Team = "Technical Support",
                    Level = EmployeeType.Support
                },
                new Employee
                {
                    UserName = "emma.support@ticketmasala.com",
                    Email = "emma.support@ticketmasala.com",
                    EmailConfirmed = true,
                    FirstName = "Emma",
                    LastName = "Taylor",
                    Phone = "+1-555-0301",
                    Team = "Technical Support",
                    Level = EmployeeType.Support
                },
                new Employee
                {
                    UserName = "robert.finance@ticketmasala.com",
                    Email = "robert.finance@ticketmasala.com",
                    EmailConfirmed = true,
                    FirstName = "Robert",
                    LastName = "Anderson",
                    Phone = "+1-555-0400",
                    Team = "Finance",
                    Level = EmployeeType.Finance
                }
            };

            foreach (var employee in employees)
            {
                var result = await _userManager.CreateAsync(employee, "Employee123!");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(employee, Constants.RoleEmployee);
                    _logger.LogInformation("Created employee user: {Email}", employee.Email);
                }
                else
                {
                    _logger.LogError("Failed to create employee user {Email}: {Errors}", 
                        employee.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        private async Task CreateCustomerUsers()
        {
            var customers = new[]
            {
                new Customer
                {
                    UserName = "alice.customer@example.com",
                    Email = "alice.customer@example.com",
                    EmailConfirmed = true,
                    FirstName = "Alice",
                    LastName = "Smith",
                    Phone = "+1-555-1000",
                    Code = "CUST001"
                },
                new Customer
                {
                    UserName = "bob.jones@example.com",
                    Email = "bob.jones@example.com",
                    EmailConfirmed = true,
                    FirstName = "Bob",
                    LastName = "Jones",
                    Phone = "+1-555-1001",
                    Code = "CUST002"
                },
                new Customer
                {
                    UserName = "carol.white@techcorp.com",
                    Email = "carol.white@techcorp.com",
                    EmailConfirmed = true,
                    FirstName = "Carol",
                    LastName = "White",
                    Phone = "+1-555-1002",
                    Code = "CUST003"
                },
                new Customer
                {
                    UserName = "daniel.brown@startup.io",
                    Email = "daniel.brown@startup.io",
                    EmailConfirmed = true,
                    FirstName = "Daniel",
                    LastName = "Brown",
                    Phone = "+1-555-1003",
                    Code = "CUST004"
                },
                new Customer
                {
                    UserName = "emily.davis@enterprise.net",
                    Email = "emily.davis@enterprise.net",
                    EmailConfirmed = true,
                    FirstName = "Emily",
                    LastName = "Davis",
                    Phone = "+1-555-1004",
                    Code = "CUST005"
                }
            };

            foreach (var customer in customers)
            {
                var result = await _userManager.CreateAsync(customer, "Customer123!");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(customer, Constants.RoleCustomer);
                    _logger.LogInformation("Created customer user: {Email}", customer.Email);
                }
                else
                {
                    _logger.LogError("Failed to create customer user {Email}: {Errors}", 
                        customer.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        private async Task CreateSampleProjects()
        {
            var customer1 = await _context.Customers.FirstAsync(c => c.Email == "alice.customer@example.com");
            var customer2 = await _context.Customers.FirstAsync(c => c.Email == "carol.white@techcorp.com");
            var customer3 = await _context.Customers.FirstAsync(c => c.Email == "daniel.brown@startup.io");

            var pm1 = await _context.Employees.FirstAsync(e => e.Email == "mike.pm@ticketmasala.com");
            var pm2 = await _context.Employees.FirstAsync(e => e.Email == "lisa.pm@ticketmasala.com");

            var projects = new[]
            {
                new Project
                {
                    Name = "Website Redesign",
                    Description = "Complete redesign of the company website with modern UI/UX",
                    Status = Status.InProgress,
                    Customer = customer1,
                    ProjectManager = pm1,
                    CompletionTarget = DateTime.UtcNow.AddMonths(2),
                    CreatorGuid = Guid.Parse((await _userManager.FindByEmailAsync("admin@ticketmasala.com"))!.Id)
                },
                new Project
                {
                    Name = "Mobile App Development",
                    Description = "Develop iOS and Android mobile applications for customer portal",
                    Status = Status.InProgress,
                    Customer = customer2,
                    ProjectManager = pm2,
                    CompletionTarget = DateTime.UtcNow.AddMonths(4),
                    CreatorGuid = Guid.Parse((await _userManager.FindByEmailAsync("admin@ticketmasala.com"))!.Id)
                },
                new Project
                {
                    Name = "Cloud Migration",
                    Description = "Migrate on-premise infrastructure to AWS cloud",
                    Status = Status.Pending,
                    Customer = customer3,
                    ProjectManager = pm1,
                    CompletionTarget = DateTime.UtcNow.AddMonths(6),
                    CreatorGuid = Guid.Parse((await _userManager.FindByEmailAsync("admin@ticketmasala.com"))!.Id)
                },
                new Project
                {
                    Name = "CRM Integration",
                    Description = "Integrate Salesforce CRM with internal systems",
                    Status = Status.Completed,
                    Customer = customer1,
                    ProjectManager = pm2,
                    CompletionTarget = DateTime.UtcNow.AddMonths(-1),
                    CompletionDate = DateTime.UtcNow.AddDays(-5),
                    CreatorGuid = Guid.Parse((await _userManager.FindByEmailAsync("admin@ticketmasala.com"))!.Id)
                }
            };

            _context.Projects.AddRange(projects);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created {Count} sample projects", projects.Length);
        }

        private async Task CreateSampleTickets()
        {
            var project1 = await _context.Projects.FirstAsync(p => p.Name == "Website Redesign");
            var project2 = await _context.Projects.FirstAsync(p => p.Name == "Mobile App Development");
            
            var customer1 = await _context.Customers.FirstAsync(c => c.Email == "alice.customer@example.com");
            var customer2 = await _context.Customers.FirstAsync(c => c.Email == "carol.white@techcorp.com");

            var support1 = await _context.Employees.FirstAsync(e => e.Email == "david.support@ticketmasala.com");
            var support2 = await _context.Employees.FirstAsync(e => e.Email == "emma.support@ticketmasala.com");

            var tickets = new[]
            {
                new Ticket
                {
                    Description = "Design homepage mockup",
                    TicketStatus = Status.Completed,
                    TicketType = TicketType.Subtask,
                    Customer = customer1,
                    Responsible = support1,
                    CompletionTarget = DateTime.UtcNow.AddDays(7),
                    CompletionDate = DateTime.UtcNow.AddDays(-2),
                    Comments = new List<string> { "Initial design completed", "Client approved the mockup" },
                    CreatorGuid = Guid.Parse(customer1.Id)
                },
                new Ticket
                {
                    Description = "Implement responsive navigation menu",
                    TicketStatus = Status.InProgress,
                    TicketType = TicketType.Subtask,
                    Customer = customer1,
                    Responsible = support2,
                    CompletionTarget = DateTime.UtcNow.AddDays(14),
                    Comments = new List<string> { "Started implementation", "Mobile view needs adjustment" },
                    CreatorGuid = Guid.Parse(customer1.Id)
                },
                new Ticket
                {
                    Description = "Setup authentication system",
                    TicketStatus = Status.Assigned,
                    TicketType = TicketType.Subtask,
                    Customer = customer2,
                    Responsible = support1,
                    CompletionTarget = DateTime.UtcNow.AddDays(21),
                    Comments = new List<string> { "Analyzing requirements" },
                    CreatorGuid = Guid.Parse(customer2.Id)
                },
                new Ticket
                {
                    Description = "Performance optimization needed",
                    TicketStatus = Status.Pending,
                    TicketType = TicketType.ProjectRequest,
                    Customer = customer1,
                    CompletionTarget = DateTime.UtcNow.AddDays(30),
                    Comments = new List<string>(),
                    CreatorGuid = Guid.Parse(customer1.Id)
                },
                new Ticket
                {
                    Description = "Bug: Payment gateway integration fails",
                    TicketStatus = Status.InProgress,
                    TicketType = TicketType.Subtask,
                    Customer = customer2,
                    Responsible = support2,
                    CompletionTarget = DateTime.UtcNow.AddDays(3),
                    Comments = new List<string> { "Issue reproduced", "Working on fix" },
                    CreatorGuid = Guid.Parse(customer2.Id)
                }
            };

            // Assign tickets to projects
            project1.Tasks.Add(tickets[0]);
            project1.Tasks.Add(tickets[1]);
            project1.Tasks.Add(tickets[3]);
            
            project2.Tasks.Add(tickets[2]);
            project2.Tasks.Add(tickets[4]);

            _context.Tickets.AddRange(tickets);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created {Count} sample tickets", tickets.Length);
        }
    }
}
