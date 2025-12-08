using IT_Project2526.AI;
using IT_Project2526.Models;
using IT_Project2526.Observers;
using IT_Project2526.Repositories;
using IT_Project2526.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IT_Project2526.Services;

/// <summary>
/// Service responsible for project business logic.
/// Follows Information Expert and Single Responsibility principles.
/// Mirrors the TicketService pattern for architectural consistency.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly ITProjectDB _context;
    private readonly IProjectRepository _projectRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEnumerable<IProjectObserver> _observers;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        ITProjectDB context,
        IProjectRepository projectRepository,
        UserManager<ApplicationUser> userManager,
        IEnumerable<IProjectObserver> observers,
        ILogger<ProjectService> logger)
    {
        _context = context;
        _projectRepository = projectRepository;
        _userManager = userManager;
        _observers = observers;
        _logger = logger;
    }

    public async Task<IEnumerable<ProjectTicketViewModel>> GetAllProjectsAsync(string? userId, bool isCustomer)
    {
        var query = _context.Projects
            .Include(p => p.Tasks.Where(t => t.ValidUntil == null))
            .Include(p => p.ProjectManager)
            .Include(p => p.Customers)
            .Where(p => p.ValidUntil == null);

        if (isCustomer && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(p => p.Customers.Any(c => c.Id == userId));
        }

        var projects = await query.ToListAsync();

        return projects.Select(p => new ProjectTicketViewModel
        {
            ProjectDetails = new ProjectViewModel
            {
                Guid = p.Guid,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status,
                ProjectManager = p.ProjectManager,
                ProjectManagerName = p.ProjectManager != null
                    ? $"{p.ProjectManager.FirstName} {p.ProjectManager.LastName}"
                    : "Not Assigned",
                TicketCount = p.Tasks.Count
            },
            Tasks = p.Tasks.Select(t => new TicketViewModel
            {
                Guid = t.Guid,
                TicketStatus = t.TicketStatus,
                CreationDate = t.CreationDate,
            }).ToList()
        });
    }

    public async Task<ProjectTicketViewModel?> GetProjectDetailsAsync(Guid projectGuid)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .Include(p => p.Tasks.Where(t => t.ValidUntil == null))
                .ThenInclude(t => t.Responsible)
            .Include(p => p.Tasks.Where(t => t.ValidUntil == null))
                .ThenInclude(t => t.Customer)
            .Include(p => p.Customer)
            .Include(p => p.ProjectManager)
            .Include(p => p.Resources)
            .Where(p => p.Guid == projectGuid && p.ValidUntil == null)
            .FirstOrDefaultAsync();

        if (project == null)
        {
            return null;
        }

        return new ProjectTicketViewModel
        {
            ProjectDetails = new ProjectViewModel
            {
                Guid = project.Guid,
                Name = project.Name,
                Description = project.Description,
                Status = project.Status,
                ProjectManagerName = project.ProjectManager != null
                    ? $"{project.ProjectManager.FirstName} {project.ProjectManager.LastName}"
                    : "Not Assigned",
                TicketCount = project.Tasks.Count
            },
            Tasks = project.Tasks.Select(t => new TicketViewModel
            {
                Guid = t.Guid,
                Description = t.Description,
                TicketStatus = t.TicketStatus,
                ResponsibleName = t.Responsible != null
                    ? $"{t.Responsible.FirstName} {t.Responsible.LastName}"
                    : "Not Assigned",
                CustomerName = t.Customer != null
                    ? $"{t.Customer.FirstName} {t.Customer.LastName}"
                    : "Unknown",
                CompletionTarget = t.CompletionTarget,
                CreationDate = DateTime.UtcNow
            }).ToList()
        };
    }

    public async Task<NewProject?> GetProjectForEditAsync(Guid projectGuid)
    {
        var project = await _context.Projects
            .Include(p => p.Customer)
            .Where(p => p.Guid == projectGuid && p.ValidUntil == null)
            .FirstOrDefaultAsync();

        if (project == null)
        {
            return null;
        }

        return new NewProject
        {
            Guid = project.Guid,
            Name = project.Name,
            Description = project.Description,
            SelectedCustomerId = project.Customer?.Id,
            CreationDate = project.CompletionTarget,
            IsNewCustomer = false,
            CustomerList = await GetCustomerSelectListAsync(project.Customer?.Id)
        };
    }

    public async Task<Project> CreateProjectAsync(NewProject viewModel, string userId)
    {
        Customer? customer;

        if (viewModel.IsNewCustomer)
        {
            customer = new Customer
            {
                FirstName = viewModel.NewCustomerFirstName ?? string.Empty,
                LastName = viewModel.NewCustomerLastName ?? string.Empty,
                Email = viewModel.NewCustomerEmail,
                Phone = viewModel.NewCustomerPhone,
                Code = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                UserName = viewModel.NewCustomerEmail
            };

            await _userManager.CreateAsync(customer);
            await _userManager.AddToRoleAsync(customer, Utilities.Constants.RoleCustomer);
        }
        else
        {
            customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == viewModel.SelectedCustomerId);

            if (customer == null)
            {
                throw new InvalidOperationException("Selected customer not found");
            }
        }
        var roadmap = await OpenAiAPIHandler.GetOpenAIResponse(OpenAIPrompts.Steps, viewModel.Description);
        var project = new Project
        {
            Name = viewModel.Name,
            Description = viewModel.Description,
            Status = Status.Pending,
            Customer = customer,
            CompletionTarget = viewModel.CreationDate,
            CreatorGuid = Guid.Parse(userId),
            ProjectAiRoadmap = roadmap,
        };

        // Add primary customer to stakeholders
        project.Customers.Add(customer);

        // Add additional stakeholders
        if (viewModel.SelectedStakeholderIds != null && viewModel.SelectedStakeholderIds.Any())
        {
            var additionalStakeholders = await _context.Customers
                .Where(c => viewModel.SelectedStakeholderIds.Contains(c.Id))
                .ToListAsync();

            foreach (var stakeholder in additionalStakeholders)
            {
                if (stakeholder.Id != customer.Id)
                {
                    project.Customers.Add(stakeholder);
                }
            }
        }

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Apply Template
        if (viewModel.SelectedTemplateId.HasValue)
        {
            await ApplyTemplateAsync(project, viewModel.SelectedTemplateId.Value, userId, customer);
        }

        _logger.LogInformation("Project created successfully: {ProjectId}", project.Guid);

        // Notify observers
        await NotifyObserversCreatedAsync(project);

        return project;
    }

    private async Task ApplyTemplateAsync(Project project, Guid templateId, string userId, Customer customer)
    {
        var template = await _context.ProjectTemplates
            .Include(t => t.Tickets)
            .FirstOrDefaultAsync(t => t.Guid == templateId);

        if (template != null)
        {
            foreach (var templateTicket in template.Tickets)
            {
                var summary = await OpenAiAPIHandler.GetOpenAIResponse(OpenAIPrompts.Summary,templateTicket.Description);

                var ticket = new Ticket
                {
                    Guid = Guid.NewGuid(),
                    Description = templateTicket.Description,
                    EstimatedEffortPoints = templateTicket.EstimatedEffortPoints,
                    PriorityScore = (double)templateTicket.Priority * 25,
                    TicketType = templateTicket.TicketType,
                    TicketStatus = Status.Pending,
                    CreationDate = DateTime.UtcNow,
                    CreatorGuid = Guid.Parse(userId),
                    Customer = customer,
                    CustomerId = customer.Id,
                    Project = project,
                    ProjectGuid = project.Guid,
                    AiSummary = summary,
                };
                _context.Tickets.Add(ticket);
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> UpdateProjectAsync(Guid projectGuid, NewProject viewModel)
    {
        var project = await _context.Projects
            .Include(p => p.Customer)
            .Where(p => p.Guid == projectGuid && p.ValidUntil == null)
            .FirstOrDefaultAsync();

        if (project == null)
        {
            return false;
        }

        project.Name = viewModel.Name;
        project.Description = viewModel.Description;
        project.CompletionTarget = viewModel.CreationDate;

        if (!string.IsNullOrEmpty(viewModel.SelectedCustomerId))
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == viewModel.SelectedCustomerId);

            if (customer != null)
            {
                project.Customer = customer;
            }
        }

        _context.Projects.Update(project);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Project updated successfully: {ProjectId}", projectGuid);

        // Notify observers
        await NotifyObserversUpdatedAsync(project);

        return true;
    }

    public async Task<List<SelectListItem>> GetCustomerSelectListAsync(string? selectedCustomerId = null)
    {
        var customers = await _context.Customers.ToListAsync();
        return customers.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = $"{c.FirstName} {c.LastName}",
            Selected = selectedCustomerId != null && c.Id == selectedCustomerId
        }).ToList();
    }

    public async Task<List<SelectListItem>> GetStakeholderSelectListAsync()
    {
        var customers = await _context.Customers.ToListAsync();
        return customers.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = $"{c.FirstName} {c.LastName}"
        }).ToList();
    }

    public async Task<List<SelectListItem>> GetTemplateSelectListAsync()
    {
        var templates = await _context.ProjectTemplates.ToListAsync();
        return templates.Select(t => new SelectListItem
        {
            Value = t.Guid.ToString(),
            Text = t.Name
        }).ToList();
    }

    /// <summary>
    /// Notify all observers that a project was created
    /// </summary>
    private async Task NotifyObserversCreatedAsync(Project project)
    {
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnProjectCreatedAsync(project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Observer {ObserverType} failed for project creation {ProjectId}",
                    observer.GetType().Name, project.Guid);
            }
        }
    }

    /// <summary>
    /// Notify all observers that a project was updated
    /// </summary>
    private async Task NotifyObserversUpdatedAsync(Project project)
    {
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnProjectUpdatedAsync(project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Observer {ObserverType} failed for project update {ProjectId}",
                    observer.GetType().Name, project.Guid);
            }
        }
    }

    /// <summary>
    /// Prepare ViewModel for creating a project from a ticket
    /// </summary>
    public async Task<CreateProjectFromTicketViewModel?> PrepareCreateFromTicketViewModelAsync(Guid ticketId)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.Guid == ticketId);

        if (ticket == null)
        {
            return null;
        }

        // Get GERDA PM recommendation
        string? recommendedPMId = null;
        string? recommendedPMName = null;
        
        // Try to get PM recommendation from dispatching service (if available via DI)
        // For now, use workload-based fallback
        var employees = await _context.Users.OfType<Employee>().ToListAsync();
        var pmProjectCounts = await _context.Projects
            .Where(p => p.ProjectManagerId != null)
            .Where(p => p.Status != Status.Completed && p.Status != Status.Failed)
            .GroupBy(p => p.ProjectManagerId)
            .Select(g => new { PMId = g.Key!, Count = g.Count() })
            .ToDictionaryAsync(x => x.PMId, x => x.Count);

        var bestPM = employees
            .Select(e => new { Employee = e, Count = pmProjectCounts.GetValueOrDefault(e.Id, 0) })
            .OrderBy(x => x.Count)
            .FirstOrDefault();

        if (bestPM != null)
        {
            recommendedPMId = bestPM.Employee.Id;
            recommendedPMName = $"{bestPM.Employee.FirstName} {bestPM.Employee.LastName}";
        }

        // Extract subject from description (first line or first 100 chars)
        var subject = ticket.Description.Split('\n')[0];
        if (subject.Length > 100) subject = subject.Substring(0, 100) + "...";

        return new CreateProjectFromTicketViewModel
        {
            TicketId = ticketId,
            TicketDescription = ticket.Description,
            CustomerName = ticket.Customer != null 
                ? $"{ticket.Customer.FirstName} {ticket.Customer.LastName}" 
                : null,
            CustomerId = ticket.CustomerId,
            ProjectName = $"Project: {subject}",
            ProjectDescription = ticket.Description,
            RecommendedPMId = recommendedPMId,
            RecommendedPMName = recommendedPMName,
            SelectedPMId = recommendedPMId, // Pre-select the recommendation
            TargetCompletionDate = DateTime.UtcNow.AddDays(30),
            TemplateList = new SelectList(await GetTemplateSelectListAsync(), "Value", "Text"),
            ProjectManagerList = await GetEmployeeSelectListAsync(recommendedPMId)
        };
    }

    /// <summary>
    /// Create a project from an existing ticket
    /// </summary>
    public async Task<Guid?> CreateProjectFromTicketAsync(CreateProjectFromTicketViewModel viewModel, string userId)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.Guid == viewModel.TicketId);

        if (ticket == null)
        {
            _logger.LogWarning("Ticket not found for project creation: {TicketId}", viewModel.TicketId);
            return null;
        }

        // Get customer
        var customer = ticket.Customer;
        if (customer == null && !string.IsNullOrEmpty(viewModel.CustomerId))
        {
            customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == viewModel.CustomerId);
        }

        // Create project
        var roadmap = await OpenAiAPIHandler.GetOpenAIResponse(OpenAIPrompts.Steps, ticket.Description);

        var project = new Project
        {
            Name = viewModel.ProjectName,
            Description = viewModel.ProjectDescription ?? ticket.Description,
            Status = Status.Pending,
            Customer = customer,
            CompletionTarget = viewModel.TargetCompletionDate,
            CreatorGuid = Guid.Parse(userId),
            ProjectManagerId = viewModel.SelectedPMId,
            ProjectAiRoadmap = roadmap,
        };

        // Add customer as stakeholder
        if (customer != null)
        {
            project.Customers.Add(customer);
        }

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Apply template if selected
        if (viewModel.SelectedTemplateId.HasValue && customer != null)
        {
            await ApplyTemplateAsync(project, viewModel.SelectedTemplateId.Value, userId, customer);
        }

        // Link the original ticket to this project
        ticket.ProjectGuid = project.Guid;
        ticket.Project = project;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Project {ProjectId} created from ticket {TicketId}", project.Guid, viewModel.TicketId);

        // Notify observers
        await NotifyObserversCreatedAsync(project);

        return project.Guid;
    }

    /// <summary>
    /// Get employee dropdown list for PM selection
    /// </summary>
    public async Task<SelectList> GetEmployeeSelectListAsync(string? selectedId = null)
    {
        var employees = await _context.Users.OfType<Employee>().ToListAsync();
        var items = employees.Select(e => new SelectListItem
        {
            Value = e.Id,
            Text = $"{e.FirstName} {e.LastName}",
            Selected = selectedId != null && e.Id == selectedId
        }).ToList();

        return new SelectList(items, "Value", "Text", selectedId);
    }
}
