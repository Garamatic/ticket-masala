using IT_Project2526.Models;
using IT_Project2526.ViewModels;
using IT_Project2526.Repositories;
using IT_Project2526.Observers;
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

        var project = new Project
        {
            Name = viewModel.Name,
            Description = viewModel.Description,
            Status = Status.Pending,
            Customer = customer,
            CompletionTarget = viewModel.CreationDate,
            CreatorGuid = Guid.Parse(userId)
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
                    ProjectGuid = project.Guid
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
}
