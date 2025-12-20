using TicketMasala.Web.Models;
using TicketMasala.Web.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace TicketMasala.Web.Data;

public class DbSeeder
{
    private readonly MasalaDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<DbSeeder> _logger;
    private readonly IWebHostEnvironment _environment;

    public DbSeeder(MasalaDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<DbSeeder> logger, IWebHostEnvironment environment)
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
            _logger.LogInformation("Ensuring database exists (SQLite EnsureCreated)...");
            var created = await _context.Database.EnsureCreatedAsync();
            _logger.LogInformation("EnsureCreatedAsync result: {Created}", created);

            // Verify tables exist
            if (!await CheckTablesExistAsync())
            {
                _logger.LogError("CRITICAL: Tables still don't exist after EnsureCreatedAsync!");
                throw new Exception("Failed to create database tables");
            }

            await EnsureRolesExistAsync();

            var userCount = await _context.Users.CountAsync();
            _logger.LogInformation("Current user count in database: {UserCount}", userCount);

            // Create Project Templates and Knowledge Base (Always run these)
            await CreateProjectTemplates();
            await SeedKnowledgeBaseAsync();

            if (userCount > 0)
            {
                _logger.LogWarning("Database already contains {UserCount} users. Skipping user/project seed.", userCount);
                return;
            }

            _logger.LogInformation("Database is empty. Loading seed data from configuration...");
            var seedConfig = await LoadSeedConfigurationAsync();

            if (seedConfig == null)
            {
                _logger.LogError("Failed to load seed configuration. Aborting seed.");
                return;
            }

            // Create Users
            _logger.LogInformation("Creating users...");
            await CreateUsersAsync(seedConfig.Admins, Constants.RoleAdmin, "Admin123!");
            await CreateEmployeesAsync(seedConfig.Employees, "Employee123!");
            await CreateUsersAsync(seedConfig.Customers, Constants.RoleCustomer, "Customer123!");

            // Create Projects (WorkContainers)
            _logger.LogInformation("Creating projects (WorkContainers)...");
            await CreateProjectsAsync(seedConfig.WorkContainers);

            // Create Unassigned Tickets (WorkItems) for GERDA
            _logger.LogInformation("Creating unassigned tickets (WorkItems) for GERDA testing...");
            await CreateUnassignedTicketsAsync(seedConfig.UnassignedWorkItems);

            _logger.LogInformation("========== DATABASE SEEDING COMPLETED SUCCESSFULLY! ==========");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "========== ERROR DURING DATABASE SEEDING ==========");
            throw;
        }
    }

    private async Task<SeedConfig?> LoadSeedConfigurationAsync()
    {
        // Use centralized configuration path resolution
        var seedFilePath = TicketMasala.Web.Configuration.ConfigurationPaths.GetConfigFilePath(
            _environment.ContentRootPath,
            "seed_data.json");

        if (!File.Exists(seedFilePath))
        {
            _logger.LogWarning("Seed data file not found at: {Path}", seedFilePath);
            return null;
        }

        try
        {
            _logger.LogInformation("Loading seed data from: {Path}", seedFilePath);
            var json = await File.ReadAllTextAsync(seedFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            return JsonSerializer.Deserialize<SeedConfig>(json, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing seed data JSON from {Path}", seedFilePath);
            return null;
        }
    }

    private async Task CreateUsersAsync(List<SeedUser> users, string role, string defaultPassword)
    {
        if (users == null || users.Count == 0)
        {
            _logger.LogWarning("No users provided for role {Role}. Skipping.", role);
            return;
        }
        foreach (var userDto in users)
        {
            var user = new ApplicationUser
            {
                UserName = userDto.UserName,
                Email = userDto.Email,
                EmailConfirmed = true,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Phone = userDto.Phone,
                Code = userDto.Code
            };

            var result = await _userManager.CreateAsync(user, defaultPassword);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                _logger.LogInformation("Created {Role} user: {Email}", role, user.Email);
            }
            else
            {
                _logger.LogError("Failed to create {Role} user {Email}: {Errors}",
                    role, user.Email, string.Join(", ", result.Errors?.Select(e => e.Description) ?? new string[0]));
            }
        }
    }

    private async Task CreateEmployeesAsync(List<SeedUser> employees, string defaultPassword)
    {
        foreach (var empDto in employees)
        {
            var employee = new Employee
            {
                UserName = empDto.UserName,
                Email = empDto.Email,
                EmailConfirmed = true,
                FirstName = empDto.FirstName,
                LastName = empDto.LastName,
                Phone = empDto.Phone,
                Team = empDto.Team ?? string.Empty,
                Level = empDto.Level ?? EmployeeType.Support,
                // GERDA Fields
                Language = empDto.Language,
                Specializations = empDto.Specializations,
                MaxCapacityPoints = empDto.MaxCapacityPoints ?? 0,
                Region = empDto.Region
            };

            var result = await _userManager.CreateAsync(employee, defaultPassword);
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

    private async Task CreateProjectsAsync(List<SeedWorkContainer> workContainers)
    {
        var adminUser = await _userManager.FindByEmailAsync("admin@ticketmasala.com");
        var adminGuid = adminUser != null ? Guid.Parse(adminUser.Id) : Guid.NewGuid();

        foreach (var wc in workContainers)
        {
            var customer = await _context.Users.FirstOrDefaultAsync(u => u.Email == wc.CustomerEmail);
            var pm = !string.IsNullOrEmpty(wc.ProjectManagerEmail)
                ? await _context.Employees.FirstOrDefaultAsync(e => e.Email == wc.ProjectManagerEmail)
                : null;

            if (customer == null)
            {
                _logger.LogWarning("Skipping project {Name}: Customer {Email} not found", wc.Name, wc.CustomerEmail);
                continue;
            }

            var project = new Project
            {
                Name = wc.Name,
                Description = wc.Description,
                Status = wc.Status,
                Customer = customer,
                ProjectManager = pm,
                CompletionTarget = DateTime.UtcNow.AddMonths(wc.CompletionTargetMonths),
                CompletionDate = wc.CompletedDaysAgo.HasValue ? DateTime.UtcNow.AddDays(-wc.CompletedDaysAgo.Value) : null,
                CreatorGuid = adminGuid
            };
            _context.Projects.Add(project);
            await _context.SaveChangesAsync(); // Save to get Project Guid

            // Add Tickets (WorkItems)
            if (wc.WorkItems.Any())
            {
                foreach (var wi in wc.WorkItems)
                {
                    var responsible = !string.IsNullOrEmpty(wi.ResponsibleEmail)
                         ? await _context.Employees.FirstOrDefaultAsync(e => e.Email == wi.ResponsibleEmail)
                         : null;

                    // Default creator to customer if not specified
                    var creatorGuid = Guid.Parse(customer.Id);

                    var ticket = new Ticket
                    {
                        Description = wi.Description,
                        TicketStatus = wi.Status,
                        TicketType = wi.Type,
                        Customer = customer,
                        Responsible = responsible,
                        CompletionTarget = DateTime.UtcNow.AddDays(wi.CompletionTargetDays),
                        CompletionDate = wi.CompletionDaysAgo.HasValue ? DateTime.UtcNow.AddDays(-wi.CompletionDaysAgo.Value) : null,
                        CreatorGuid = creatorGuid,
                        ProjectGuid = project.Guid,
                        EstimatedEffortPoints = (int)(wi.EstimatedEffortPoints ?? 0),
                        PriorityScore = wi.PriorityScore ?? 0,
                        GerdaTags = wi.GerdaTags
                    };

                    // Add Comments
                    if (wi.Comments.Any())
                    {
                        ticket.Comments = new List<TicketComment>();
                        foreach (var cm in wi.Comments)
                        {
                            var author = await _context.Users.FirstOrDefaultAsync(u => u.Email == cm.AuthorEmail);
                            if (author != null)
                            {
                                ticket.Comments.Add(new TicketComment
                                {
                                    Body = cm.Body,
                                    AuthorId = author.Id,
                                    CreatedAt = DateTime.UtcNow.AddDays(-cm.CreatedDaysAgo)
                                });
                            }
                        }
                    }

                    _context.Tickets.Add(ticket);
                }
                await _context.SaveChangesAsync();
            }
        }
    }

    private async Task CreateUnassignedTicketsAsync(List<SeedWorkItem> workItems)
    {
        foreach (var wi in workItems)
        {
            var customer = await _context.Users.FirstOrDefaultAsync(u => u.Email == wi.CustomerEmail);
            if (customer == null) continue;

            var ticket = new Ticket
            {
                Description = wi.Description,
                TicketStatus = wi.Status,
                TicketType = wi.Type,
                Customer = customer,
                CompletionTarget = DateTime.UtcNow.AddDays(wi.CompletionTargetDays),
                CreatorGuid = Guid.Parse(customer.Id),
                EstimatedEffortPoints = (int)(wi.EstimatedEffortPoints ?? 0),
                PriorityScore = wi.PriorityScore ?? 0,
                GerdaTags = wi.GerdaTags
            };

            if (wi.Comments.Any())
            {
                ticket.Comments = new List<TicketComment>();
                foreach (var cm in wi.Comments)
                {
                    var author = await _context.Users.FirstOrDefaultAsync(u => u.Email == cm.AuthorEmail);
                    if (author != null)
                    {
                        ticket.Comments.Add(new TicketComment
                        {
                            Body = cm.Body,
                            AuthorId = author.Id,
                            CreatedAt = DateTime.UtcNow.AddDays(-cm.CreatedDaysAgo)
                        });
                    }
                }
            }

            _context.Tickets.Add(ticket);
        }
        await _context.SaveChangesAsync();
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

    private async Task SeedKnowledgeBaseAsync()
    {
        if (await _context.KnowledgeBaseArticles.AnyAsync())
        {
            _logger.LogInformation("Knowledge Base already has articles. Skipping seed.");
            return;
        }

        _logger.LogInformation("Seeding Knowledge Base...");

        var adminUser = await _userManager.FindByEmailAsync("admin@ticketmasala.com");
        var authorId = adminUser?.Id;

        var articles = new List<KnowledgeBaseArticle>
            {
                new KnowledgeBaseArticle
                {
                    Id = Guid.NewGuid(),
                    Title = "Getting Started with Ticket Masala",
                    Content = @"Welcome to Ticket Masala! This guide will help you get up and running quickly.
                    
                    **1. Dashboard Overview**
                    Your dashboard is the central hub for all your activities. Use the sidebar to navigate between Projects, Tickets, and Team views.
                    
                    **2. Creating your first Project**
                    Navigate to the 'Projects' page and click 'New Project'. giving it a name and selecting a template.
                    
                    **3. Raising a Ticket**
                    Need help? Click 'Create Ticket' from the top bar or 'New Ticket' from the Tickets page.
                    
                    For more help, contact your administrator.",
                    Tags = "getting-started,guide,onboarding",
                    AuthorId = authorId,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new KnowledgeBaseArticle
                {
                    Id = Guid.NewGuid(),
                    Title = "Understanding Ticket Priorities",
                    Content = @"Ticket Masala uses a 4-level priority system to ensure critical issues are addressed first.
                    
                    **LOW**: Cosmetic issues, minor bugs that do not affect functionality.
                    **MEDIUM**: Standard bugs, functionality issues with workarounds. default for most tickets.
                    **HIGH**: Major functionality broken, no workaround available.
                    **CRITICAL**: System down, data loss risk, security vulnerability. Immediate action required.
                    
                    GERDA AI automatically analyzes your ticket description to suggest an appropriate priority.",
                    Tags = "priority,guide,best-practices",
                    AuthorId = authorId,
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                },
                new KnowledgeBaseArticle
                {
                    Id = Guid.NewGuid(),
                    Title = "How to Reset Your Password",
                    Content = @"If you've forgotten your password, follow these steps:
                    
                    1. Go to the Login page.
                    2. Click the 'Forgot your password?' link.
                    3. Enter your email address and click 'Submit'.
                    4. Check your email for a reset link.
                    5. Click the link and enter a new secure password.
                    
                    If you don't receive an email within 5 minutes, check your spam folder.",
                    Tags = "account,security,password",
                    AuthorId = authorId,
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                },
                new KnowledgeBaseArticle
                {
                    Id = Guid.NewGuid(),
                    Title = "GERDA AI Features Explained",
                    Content = @"GERDA (General Intelligent Routing & Dispatch Agent) is the AI brain of Ticket Masala.
                    
                    **Key Features:**
                    * **Smart Routing**: Automatically assigns tickets to the best-suited agent based on skills and workload.
                    * **Effort Estimation**: Predicts story points for new tickets based on historical data.
                    * **Sentiment Analysis**: Detects frustrated customers to flag tickets for senior review.
                    * **Duplicate Detection**: Warns you if a similar ticket already exists.
                    
                    GERDA saves your team hours of manual triage every week.",
                    Tags = "ai,gerda,features",
                    AuthorId = authorId,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new KnowledgeBaseArticle
                {
                    Id = Guid.NewGuid(),
                    Title = "Advanced Search Tips",
                    Content = @"Find what you need faster with these search tricks:
                    
                    * Use quotes for exact phrases: `""login error""`
                    * Filter by status: `status:open`
                    * Filter by assignee: `assigned:jane`
                    * Combine filters: `status:open assigned:me priority:high`
                    
                    Search is available globally from the top navigation bar.",
                    Tags = "search,tips,productivity",
                    AuthorId = authorId,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                }
            };

        await _context.KnowledgeBaseArticles.AddRangeAsync(articles);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Added {Count} articles to Knowledge Base.", articles.Count);
    }

}
