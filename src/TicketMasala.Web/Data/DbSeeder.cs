using TicketMasala.Web.Models;
using TicketMasala.Web.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TicketMasala.Web.Data;
    public class DbSeeder
    {
        private readonly ITProjectDB _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<DbSeeder> _logger;
        private readonly IWebHostEnvironment _environment;

        public DbSeeder(ITProjectDB context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<DbSeeder> logger, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
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

        private async Task EnsureRolesExistAsync()
        {
            // EnsureCreated doesn't run HasData() seeding, so we need to create roles manually
            var roles = new[] { Constants.RoleAdmin, Constants.RoleEmployee, Constants.RoleCustomer };
            
            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Created role: {Role}", roleName);
                    }
                    else
                    {
                        _logger.LogError("Failed to create role {Role}: {Errors}", 
                            roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    _logger.LogInformation("Role already exists: {Role}", roleName);
                }
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
                    
                    // EnsureCreated doesn't run HasData(), so we need to create roles manually
                    await EnsureRolesExistAsync();
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
                
                // Create Project Templates
                _logger.LogInformation("Creating project templates...");
                await CreateProjectTemplates();

                if (userCount > 0)
                {
                    _logger.LogWarning("Database already contains {UserCount} users. Skipping user/project seed.", userCount);
                    _logger.LogInformation("If you want to re-seed users, please delete all users first or drop the database");
                    
                    _logger.LogInformation("========== DATABASE SEEDING COMPLETED SUCCESSFULLY! ==========");
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

                // Create Unassigned Tickets for GERDA Dispatch Testing
                _logger.LogInformation("Creating unassigned tickets for GERDA testing...");
                await CreateUnassignedTicketsForGerdaTesting();



                _logger.LogInformation("========== DATABASE SEEDING COMPLETED SUCCESSFULLY! ==========");
                _logger.LogInformation("You can now login with any of the test accounts");
                _logger.LogInformation("Admin: admin@ticketmasala.com / Admin123!");
                _logger.LogInformation("Employee: mike.pm@ticketmasala.com / Employee123!");
                _logger.LogInformation("Customer: alice.customer@example.com / Customer123!");
                _logger.LogInformation("Created {Count} unassigned tickets for GERDA Dispatch testing", 15);
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
                    Level = EmployeeType.ProjectManager,
                    // GERDA AI Fields
                    Language = "EN",
                    Specializations = "[\"Project Management\",\"Agile\",\"Risk Management\"]",
                    MaxCapacityPoints = 50,
                    Region = "North America"
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
                    Level = EmployeeType.ProjectManager,
                    // GERDA AI Fields
                    Language = "EN,ZH",
                    Specializations = "[\"Project Management\",\"DevOps\",\"Infrastructure\"]",
                    MaxCapacityPoints = 45,
                    Region = "Asia-Pacific"
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
                    Level = EmployeeType.Support,
                    // GERDA AI Fields
                    Language = "EN,ES",
                    Specializations = "[\"Hardware Support\",\"Network Troubleshooting\",\"System Outage\"]",
                    MaxCapacityPoints = 40,
                    Region = "North America"
                },
                new Employee
                {
                    UserName = "claude.support@ticketmasala.com",
                    Email = "claude.support@ticketmasala.com",
                    EmailConfirmed = true,
                    FirstName = "Claude",
                    LastName = "Dubois",
                    Phone = "+32-555-0400",
                    Team = "European Support",
                    Level = EmployeeType.Support,
                    // GERDA AI Fields
                    Language = "FR,EN",
                    Specializations = "[\"Software Troubleshooting\",\"Refund Request\",\"Tax Processing\"]",
                    MaxCapacityPoints = 45,
                    Region = "Europe"
                },
                new Employee
                {
                    UserName = "pieter.support@ticketmasala.com",
                    Email = "pieter.support@ticketmasala.com",
                    EmailConfirmed = true,
                    FirstName = "Pieter",
                    LastName = "Vandenberg",
                    Phone = "+31-555-0500",
                    Team = "Benelux Support",
                    Level = EmployeeType.Support,
                    // GERDA AI Fields
                    Language = "NL,EN,FR",
                    Specializations = "[\"Project Management\",\"Agile\",\"Infrastructure\"]",
                    MaxCapacityPoints = 42,
                    Region = "Europe"
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
                    Level = EmployeeType.Support,
                    // GERDA AI Fields
                    Language = "EN,FR",
                    Specializations = "[\"Software Troubleshooting\",\"Bug Triage\",\"System Outage\"]",
                    MaxCapacityPoints = 35,
                    Region = "Europe"
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
                    Level = EmployeeType.Finance,
                    // GERDA AI Fields
                    Language = "EN",
                    Specializations = "[\"Payroll\",\"Tax Processing\",\"Refund Request\"]",
                    MaxCapacityPoints = 30,
                    Region = "North America"
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
                    Comments = new List<TicketComment> 
                    { 
                        new TicketComment { Body = "Initial design completed", AuthorId = support1.Id, CreatedAt = DateTime.UtcNow.AddDays(-3) },
                        new TicketComment { Body = "Client approved the mockup", AuthorId = customer1.Id, CreatedAt = DateTime.UtcNow.AddDays(-2) }
                    },
                    CreatorGuid = Guid.Parse(customer1.Id),
                    ProjectGuid = project1.Guid
                },
                new Ticket
                {
                    Description = "Implement responsive navigation menu",
                    TicketStatus = Status.InProgress,
                    TicketType = TicketType.Subtask,
                    Customer = customer1,
                    Responsible = support2,
                    CompletionTarget = DateTime.UtcNow.AddDays(14),
                    Comments = new List<TicketComment> 
                    { 
                        new TicketComment { Body = "Started implementation", AuthorId = support2.Id, CreatedAt = DateTime.UtcNow.AddDays(-1) },
                        new TicketComment { Body = "Mobile view needs adjustment", AuthorId = support2.Id, CreatedAt = DateTime.UtcNow }
                    },
                    CreatorGuid = Guid.Parse(customer1.Id),
                    ProjectGuid = project1.Guid
                },
                new Ticket
                {
                    Description = "Setup authentication system",
                    TicketStatus = Status.Assigned,
                    TicketType = TicketType.Subtask,
                    Customer = customer2,
                    Responsible = support1,
                    CompletionTarget = DateTime.UtcNow.AddDays(21),
                    Comments = new List<TicketComment> 
                    { 
                        new TicketComment { Body = "Analyzing requirements", AuthorId = support1.Id, CreatedAt = DateTime.UtcNow }
                    },
                    CreatorGuid = Guid.Parse(customer2.Id),
                    ProjectGuid = project2.Guid
                },
                new Ticket
                {
                    Description = "Performance optimization needed",
                    TicketStatus = Status.Pending,
                    TicketType = TicketType.ProjectRequest,
                    Customer = customer1,
                    CompletionTarget = DateTime.UtcNow.AddDays(30),
                    Comments = new List<TicketComment>(),
                    CreatorGuid = Guid.Parse(customer1.Id),
                    ProjectGuid = project1.Guid
                },
                new Ticket
                {
                    Description = "Bug: Payment gateway integration fails",
                    TicketStatus = Status.InProgress,
                    TicketType = TicketType.Subtask,
                    Customer = customer2,
                    Responsible = support2,
                    CompletionTarget = DateTime.UtcNow.AddDays(3),
                    Comments = new List<TicketComment> 
                    { 
                        new TicketComment { Body = "Issue reproduced", AuthorId = support2.Id, CreatedAt = DateTime.UtcNow.AddHours(-2) },
                        new TicketComment { Body = "Working on fix", AuthorId = support2.Id, CreatedAt = DateTime.UtcNow }
                    },
                    CreatorGuid = Guid.Parse(customer2.Id),
                    ProjectGuid = project2.Guid
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

        private async Task CreateUnassignedTicketsForGerdaTesting()
        {
            var customers = await _context.Customers.ToListAsync();
            var projects = await _context.Projects.Where(p => p.ValidUntil == null && p.Status != Status.Completed).ToListAsync();

            var unassignedTickets = new List<Ticket>
            {
                // Hardware Support tickets
                new Ticket
                {
                    Description = "Laptop screen is flickering intermittently",
                    TicketStatus = Status.Pending,
                    TicketType = TicketType.ProjectRequest,
                    Customer = customers[0],
                    CompletionTarget = DateTime.UtcNow.AddDays(2),
                    Comments = new List<TicketComment> 
                    { 
                        new TicketComment { Body = "Issue started yesterday", AuthorId = customers[0].Id }
                    },
                    CreatorGuid = Guid.Parse(customers[0].Id),
                    EstimatedEffortPoints = 3,
                    PriorityScore = 45.0,
                    GerdaTags = "Hardware"
                },
                new Ticket
                {
                    Description = "Printer not responding to print jobs",
                    TicketStatus = Status.Pending,
                    TicketType = TicketType.ProjectRequest,
                    Customer = customers[1],
                    CompletionTarget = DateTime.UtcNow.AddDays(1),
                    Comments = new List<TicketComment> 
                    { 
                        new TicketComment { Body = "Urgent - affects entire department", AuthorId = customers[1].Id }
                    },
                    CreatorGuid = Guid.Parse(customers[1].Id),
                    EstimatedEffortPoints = 2,
                    PriorityScore = 65.0,
                    GerdaTags = "Hardware,Urgent"
                },
                new Ticket
                {
                    Description = "Need replacement keyboard - keys are stuck",
                    TicketStatus = Status.Pending,
                    TicketType = TicketType.ProjectRequest,
                    Customer = customers[2],
                    CompletionTarget = DateTime.UtcNow.AddDays(3),
                    Comments = new List<TicketComment>(),
                    CreatorGuid = Guid.Parse(customers[2].Id),
                    EstimatedEffortPoints = 1,
                    PriorityScore = 25.0,
                    GerdaTags = "Hardware"
                },

                // Network Troubleshooting tickets
                new Ticket
                {
                    Description = "Cannot connect to VPN from home office",
                    TicketStatus = Status.Pending,
                    TicketType = TicketType.ProjectRequest,
                    Customer = customers[3],
                    CompletionTarget = DateTime.UtcNow.AddDays(1),
                    Comments = new List<TicketComment> 
                    { 
                        new TicketComment { Body = "Error message: Connection timeout", AuthorId = customers[3].Id }
                    },
                    CreatorGuid = Guid.Parse(customers[3].Id),
                    EstimatedEffortPoints = 5,
                    PriorityScore = 55.0,
                    GerdaTags = "Network"
                },
                new Ticket
                {
                    Description = "Slow internet connection in conference room B",
                    TicketStatus = Status.Pending,
                    TicketType = TicketType.ProjectRequest,
                    Customer = customers[4],
                    CompletionTarget = DateTime.UtcNow.AddDays(2),
                    Comments = new List<TicketComment> 
                    { 
                        new TicketComment { Body = "Speed test shows 1 Mbps instead of 100 Mbps", AuthorId = customers[4].Id }
                    },
                    CreatorGuid = Guid.Parse(customers[4].Id),
                    EstimatedEffortPoints = 3,
                    PriorityScore = 40.0,
                    GerdaTags = "Network"
                },

                // Software Troubleshooting tickets
                new Ticket
                {
                    Description = "Application crashes when opening large files",
                    TicketStatus = Status.Pending,
                    TicketType = TicketType.ProjectRequest,
                    Customer = customers[0],
                    CompletionTarget = DateTime.UtcNow.AddDays(3),
                    Comments = new List<TicketComment> 
                    { 
                        new TicketComment { Body = "Happens with files over 50MB", AuthorId = customers[0].Id }
                    },
                    CreatorGuid = Guid.Parse(customers[0].Id),
                    EstimatedEffortPoints = 8,
                    PriorityScore = 50.0,
                    GerdaTags = "Software,Bug"
                },
                new Ticket
                {
                    Description = "Email client not syncing with server",
                    TicketStatus = Status.Pending,
                    TicketType = TicketType.ProjectRequest,
                    Customer = customers[1],
                    CompletionTarget = DateTime.UtcNow.AddHours(12),
                    Comments = new List<TicketComment> 
                    { 
                        new TicketComment { Body = "Critical - missing important emails", AuthorId = customers[1].Id }
                    },
                    CreatorGuid = Guid.Parse(customers[1].Id),
                    EstimatedEffortPoints = 5,
                    PriorityScore = 75.0,
                    GerdaTags = "Software,Critical"
                },
                new Ticket
                {
                    Description = "Software update fails with error 0x80070005",
                    TicketStatus = Status.Pending,
                    TicketType = TicketType.ProjectRequest,
                    Customer = customers[2],
                    CompletionTarget = DateTime.UtcNow.AddDays(2),
                    Comments = new List<TicketComment> 
                    { 
                        new TicketComment { Body = "Tried restarting multiple times", AuthorId = customers[2].Id }
                    },
                    CreatorGuid = Guid.Parse(customers[2].Id),
                    EstimatedEffortPoints = 3,
                    PriorityScore = 35.0,
                    GerdaTags = "Software"
                },

                // Password Reset tickets
                new Ticket
                {
                    Description = "Forgot password for HR portal - need urgent reset",
                    TicketStatus = Status.Pending,
                    TicketType = TicketType.ProjectRequest,
                    Customer = customers[3],
                    CompletionTarget = DateTime.UtcNow.AddHours(6),
                    Comments = new List<TicketComment> 
                    { 
                        new TicketComment { Body = "Need to submit timesheet today", AuthorId = customers[3].Id }
                    },
                    CreatorGuid = Guid.Parse(customers[3].Id),
                    EstimatedEffortPoints = 1,
                    PriorityScore = 60.0,
                    GerdaTags = "Password"
                },
                new Ticket
                {
                    Description = "Account locked after multiple failed login attempts",
                    TicketStatus = Status.Pending,
                    TicketType = TicketType.ProjectRequest,
                    Customer = customers[4],
                    CompletionTarget = DateTime.UtcNow.AddHours(4),
                    Comments = new List<TicketComment>(),
                    CreatorGuid = Guid.Parse(customers[4].Id),
                    EstimatedEffortPoints = 1,
                    PriorityScore = 70.0,
                    GerdaTags = "Password,Locked"
                },

                // Payroll tickets
                new Ticket
                {
                    Description = "Incorrect tax deduction on last paycheck",
                    TicketStatus = Status.Pending,
                    TicketType = TicketType.ProjectRequest,
                    Customer = customers[0],
                    CompletionTarget = DateTime.UtcNow.AddDays(5),
                    Comments = new List<TicketComment> 
                    { 
                        new TicketComment { Body = "Deducted 30% instead of 25%", AuthorId = customers[0].Id }
                    },
                    CreatorGuid = Guid.Parse(customers[0].Id),
                    EstimatedEffortPoints = 5,
                    PriorityScore = 55.0,
                    GerdaTags = "Payroll,Tax"
                },
                new Ticket
                {
                    Description = "Need copy of W2 form from 2023",
                    TicketStatus = Status.Pending,
                    TicketType = TicketType.ProjectRequest,
                    Customer = customers[1],
                    CompletionTarget = DateTime.UtcNow.AddDays(7),
                    Comments = new List<TicketComment>(),
                    CreatorGuid = Guid.Parse(customers[1].Id),
                    EstimatedEffortPoints = 2,
                    PriorityScore = 30.0,
                    GerdaTags = "Payroll,Tax"
                },

                // Refund Request tickets
                new Ticket
                {
                    Description = "Request refund for cancelled subscription",
                    TicketStatus = Status.Pending,
                    TicketType = TicketType.ProjectRequest,
                    Customer = customers[2],
                    CompletionTarget = DateTime.UtcNow.AddDays(10),
                    Comments = new List<TicketComment> 
                    { 
                        new TicketComment { Body = "Cancelled on Nov 15 but charged on Dec 1", AuthorId = customers[2].Id }
                    },
                    CreatorGuid = Guid.Parse(customers[2].Id),
                    EstimatedEffortPoints = 3,
                    PriorityScore = 40.0,
                    GerdaTags = "Refund"
                },

                // DevOps/Infrastructure tickets
                new Ticket
                {
                    Description = "Setup continuous deployment pipeline for new project",
                    TicketStatus = Status.Pending,
                    TicketType = TicketType.ProjectRequest,
                    Customer = customers[3],
                    CompletionTarget = DateTime.UtcNow.AddDays(14),
                    Comments = new List<TicketComment> 
                    { 
                        new TicketComment { Body = "Need Jenkins integration with GitHub", AuthorId = customers[3].Id }
                    },
                    CreatorGuid = Guid.Parse(customers[3].Id),
                    ProjectGuid = projects.Count > 0 ? projects[0].Guid : null,
                    EstimatedEffortPoints = 13,
                    PriorityScore = 45.0,
                    GerdaTags = "DevOps,Infrastructure"
                },
                new Ticket
                {
                    Description = "Database backup taking too long - optimization needed",
                    TicketStatus = Status.Pending,
                    TicketType = TicketType.ProjectRequest,
                    Customer = customers[4],
                    CompletionTarget = DateTime.UtcNow.AddDays(7),
                    Comments = new List<TicketComment> 
                    { 
                        new TicketComment { Body = "Currently takes 6 hours, should be under 2", AuthorId = customers[4].Id }
                    },
                    CreatorGuid = Guid.Parse(customers[4].Id),
                    ProjectGuid = projects.Count > 1 ? projects[1].Guid : null,
                    EstimatedEffortPoints = 8,
                    PriorityScore = 50.0,
                    GerdaTags = "Infrastructure,Database"
                }
            };

            _context.Tickets.AddRange(unassignedTickets);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created {Count} unassigned tickets for GERDA Dispatch testing", unassignedTickets.Count);
        }
        private async Task CreateProjectTemplates()
        {
            // Define all templates
            var templates = new List<ProjectTemplate>
            {
                new ProjectTemplate
                {
                    Guid = Guid.Parse("11111111-1111-1111-1111-111111111111"), // Fixed GUIDs for idempotency
                    Name = "Standard Web Project",
                    Description = "Standard template for new web development projects",
                    Tickets = new List<TemplateTicket>
                    {
                        new TemplateTicket { Description = "Setup Development Environment", EstimatedEffortPoints = 3, Priority = Priority.High, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "Design Database Schema", EstimatedEffortPoints = 5, Priority = Priority.High, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "Create API Endpoints", EstimatedEffortPoints = 8, Priority = Priority.Medium, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "Implement Frontend UI", EstimatedEffortPoints = 13, Priority = Priority.Medium, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "Perform Unit Testing", EstimatedEffortPoints = 5, Priority = Priority.Medium, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "Deploy to Staging", EstimatedEffortPoints = 2, Priority = Priority.High, TicketType = TicketType.Task }
                    }
                },
                new ProjectTemplate
                {
                    Guid = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "Mobile App Launch",
                    Description = "Template for launching a new mobile application",
                    Tickets = new List<TemplateTicket>
                    {
                        new TemplateTicket { Description = "Design App Icon & Splash Screen", EstimatedEffortPoints = 3, Priority = Priority.Medium, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "Setup Store Listings (iOS/Android)", EstimatedEffortPoints = 5, Priority = Priority.High, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "Configure Push Notifications", EstimatedEffortPoints = 5, Priority = Priority.Medium, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "Beta Testing Coordination", EstimatedEffortPoints = 8, Priority = Priority.High, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "Final Polish & Bug Fixes", EstimatedEffortPoints = 13, Priority = Priority.Critical, TicketType = TicketType.Task }
                    }
                },
                new ProjectTemplate
                {
                    Guid = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "E-commerce Platform Launch",
                    Description = "Comprehensive template for building an online store",
                    Tickets = new List<TemplateTicket>
                    {
                        new TemplateTicket { Description = "Setup Product Catalog Structure", EstimatedEffortPoints = 5, Priority = Priority.High, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "Implement Shopping Cart Logic", EstimatedEffortPoints = 8, Priority = Priority.Critical, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "Integrate Payment Gateway (Stripe/PayPal)", EstimatedEffortPoints = 13, Priority = Priority.Critical, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "User Account & Order History", EstimatedEffortPoints = 8, Priority = Priority.High, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "Configure Shipping & Tax Rules", EstimatedEffortPoints = 5, Priority = Priority.Medium, TicketType = TicketType.Task }
                    }
                },
                new ProjectTemplate
                {
                    Guid = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = "Internal HR Tool",
                    Description = "System for managing employee data and processes",
                    Tickets = new List<TemplateTicket>
                    {
                        new TemplateTicket { Description = "Employee Database Schema Design", EstimatedEffortPoints = 5, Priority = Priority.High, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "Leave Management Module", EstimatedEffortPoints = 8, Priority = Priority.Medium, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "Payroll Integration Interface", EstimatedEffortPoints = 13, Priority = Priority.Critical, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "Role-Based Access Control Setup", EstimatedEffortPoints = 5, Priority = Priority.High, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "Generate HR Reports", EstimatedEffortPoints = 3, Priority = Priority.Low, TicketType = TicketType.Task }
                    }
                },
                new ProjectTemplate
                {
                    Guid = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Name = "Marketing Campaign",
                    Description = "Tasks for launching a new digital marketing campaign",
                    Tickets = new List<TemplateTicket>
                    {
                        new TemplateTicket { Description = "Content Strategy & Calendar", EstimatedEffortPoints = 3, Priority = Priority.High, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "Create Social Media Assets", EstimatedEffortPoints = 5, Priority = Priority.Medium, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "Setup Email Marketing Automation", EstimatedEffortPoints = 5, Priority = Priority.Medium, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "Configure Analytics & Tracking", EstimatedEffortPoints = 3, Priority = Priority.High, TicketType = TicketType.Task },
                        new TemplateTicket { Description = "Launch Ad Campaigns", EstimatedEffortPoints = 2, Priority = Priority.Critical, TicketType = TicketType.Task }
                    }
                }
            };

            var newTemplatesCount = 0;
            foreach (var template in templates)
            {
                // Check if template exists by Name to avoid duplicates
                if (!await _context.ProjectTemplates.AnyAsync(t => t.Name == template.Name))
                {
                    _context.ProjectTemplates.Add(template);
                    newTemplatesCount++;
                }
            }

            if (newTemplatesCount > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created {Count} new project templates", newTemplatesCount);
            }
            else
            {
                _logger.LogInformation("All project templates already exist. Skipping.");
            }
        }
}
