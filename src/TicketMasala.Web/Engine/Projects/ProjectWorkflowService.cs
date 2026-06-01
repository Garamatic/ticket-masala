using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Ports;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Observers;
using TicketMasala.Web.ViewModels.Projects;

namespace TicketMasala.Web.Engine.Projects;

public class ProjectWorkflowService : IProjectWorkflowService
{
    private readonly MasalaDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEnumerable<IProjectObserver> _observers;
    private readonly IAIGenerationPort _aiPort;
    private readonly IProjectTemplateService _templateService;
    private readonly ILogger<ProjectWorkflowService> _logger;
    private readonly ISystemClock _clock;

    public ProjectWorkflowService(
        MasalaDbContext context,
        UserManager<ApplicationUser> userManager,
        IEnumerable<IProjectObserver> observers,
        IAIGenerationPort aiPort,
        IProjectTemplateService templateService,
        ILogger<ProjectWorkflowService> logger,
        ISystemClock clock)
    {
        _context = context;
        _userManager = userManager;
        _observers = observers;
        _aiPort = aiPort;
        _templateService = templateService;
        _logger = logger;
        _clock = clock;
    }

    public async Task<Project> CreateProjectAsync(NewProject viewModel, string userId)
    {
        if (!Guid.TryParse(userId, out var creatorGuid))
        {
            throw new ArgumentException("Invalid user ID", nameof(userId));
        }

        ApplicationUser? customer;

        if (viewModel.IsNewCustomer)
        {
            customer = new ApplicationUser
            {
                FirstName = viewModel.NewCustomerFirstName ?? string.Empty,
                LastName = viewModel.NewCustomerLastName ?? string.Empty,
                Email = viewModel.NewCustomerEmail,
                Phone = viewModel.NewCustomerPhone,
                UserName = viewModel.NewCustomerEmail
            };

            var createResult = await _userManager.CreateAsync(customer);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create customer: {errors}");
            }

            var roleResult = await _userManager.AddToRoleAsync(customer, Constants.RoleCustomer);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to add customer role: {errors}");
            }
        }
        else
        {
            customer = await _context.Users
                .FirstOrDefaultAsync(c => c.Id == viewModel.SelectedCustomerId);

            if (customer == null)
            {
                throw new InvalidOperationException("Selected customer not found");
            }
        }

        // Generate AI roadmap for the project
        string? roadmap = null;
        try
        {
            var result = await _aiPort.CompleteAsync(
                new AICompletionRequest
                {
                    Operation = "roadmap",
                    Content = viewModel.Description,
                });
            roadmap = result.Text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate AI roadmap for project, continuing without it");
        }

        var project = new Project
        {
            Name = viewModel.Name,
            Description = viewModel.Description,
            Status = Status.Pending,
            Customer = customer,
            CustomerId = customer.Id,
            CompletionTarget = viewModel.CreationDate,
            CreatorGuid = creatorGuid,
            ProjectAiRoadmap = roadmap,
            CreationDate = _clock.UtcNow
        };

        // Set project manager if provided
        if (!string.IsNullOrEmpty(viewModel.SelectedProjectManagerId))
        {
            var manager = await _context.Users.OfType<Employee>()
                .FirstOrDefaultAsync(e => e.Id == viewModel.SelectedProjectManagerId);
            if (manager != null)
            {
                project.ProjectManager = manager;
                project.ProjectManagerId = manager.Id;
            }
        }

        // Add primary customer to stakeholders
        project.Customers.Add(customer);

        // Add additional stakeholders
        if (viewModel.SelectedStakeholderIds != null && viewModel.SelectedStakeholderIds.Any())
        {
            var additionalStakeholders = await _context.Users
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
            await _templateService.ApplyTemplateAsync(project, viewModel.SelectedTemplateId.Value);
        }

        _logger.LogInformation("Project created successfully: {ProjectId}", project.Guid);

        // Notify observers
        await NotifyObserversCreatedAsync(project);

        return project;
    }

    public async Task<Guid?> CreateProjectFromTicketAsync(CreateProjectFromTicketViewModel viewModel, string userId)
    {
        if (!Guid.TryParse(userId, out var creatorGuid))
        {
            throw new ArgumentException("Invalid user ID", nameof(userId));
        }

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
            customer = await _context.Users.FirstOrDefaultAsync(c => c.Id == viewModel.CustomerId);
        }

        // Generate AI roadmap
        string? roadmap = null;
        try
        {
            var result = await _aiPort.CompleteAsync(
                new AICompletionRequest
                {
                    Operation = "roadmap",
                    Content = ticket.Description,
                });
            roadmap = result.Text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate AI roadmap for project from ticket");
        }

        // Load project manager if provided
        Employee? manager = null;
        if (!string.IsNullOrEmpty(viewModel.SelectedPMId))
        {
            manager = await _context.Users.OfType<Employee>()
                .FirstOrDefaultAsync(e => e.Id == viewModel.SelectedPMId);
        }

        var project = new Project
        {
            Name = viewModel.ProjectName,
            Description = viewModel.ProjectDescription ?? ticket.Description,
            Status = Status.Pending,
            Customer = customer,
            CustomerId = customer?.Id,
            CompletionTarget = viewModel.TargetCompletionDate,
            CreatorGuid = creatorGuid,
            ProjectManager = manager,
            ProjectManagerId = manager?.Id,
            ProjectAiRoadmap = roadmap,
            CreationDate = _clock.UtcNow
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
            await _templateService.ApplyTemplateAsync(project, viewModel.SelectedTemplateId.Value);
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
        project.ProjectType = viewModel.ProjectType;
        project.Notes = viewModel.ProjectComment;

        // Update Project Manager
        if (!string.IsNullOrEmpty(viewModel.SelectedProjectManagerId))
        {
            var manager = await _context.Users.OfType<Employee>()
                .FirstOrDefaultAsync(e => e.Id == viewModel.SelectedProjectManagerId);

            if (manager != null)
            {
                project.ProjectManager = manager;
                project.ProjectManagerId = manager.Id;
            }
        }
        else
        {
            project.ProjectManager = null; // Unassign if cleared
            project.ProjectManagerId = null;
        }

        if (!string.IsNullOrEmpty(viewModel.SelectedCustomerId))
        {
            var customer = await _context.Users
                .FirstOrDefaultAsync(c => c.Id == viewModel.SelectedCustomerId);

            if (customer != null)
            {
                project.Customer = customer;
                project.CustomerId = customer.Id;
            }
        }
        else
        {
            project.Customer = null; // Unassign if cleared
            project.CustomerId = null;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Project updated successfully: {ProjectId}", projectGuid);

        // Notify observers
        await NotifyObserversUpdatedAsync(project);

        return true;
    }

    public async Task<bool> UpdateProjectStatusAsync(Guid projectGuid, Status status)
    {
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Guid == projectGuid && p.ValidUntil == null);

        if (project == null)
        {
            return false;
        }

        project.Status = status;
        if (status == Status.Completed)
        {
            project.CompletionDate = _clock.UtcNow;
        }
        else
        {
            project.CompletionDate = null;
        }

        await _context.SaveChangesAsync();
        await NotifyObserversUpdatedAsync(project);
        return true;
    }

    public async Task<bool> AssignProjectManagerAsync(Guid projectGuid, string managerId)
    {
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Guid == projectGuid && p.ValidUntil == null);
        if (project == null)
        {
            return false;
        }

        var manager = await _userManager.FindByIdAsync(managerId) as Employee;
        if (manager == null)
        {
            return false;
        }

        project.ProjectManager = manager;
        project.ProjectManagerId = manager.Id;
        await _context.SaveChangesAsync();
        await NotifyObserversUpdatedAsync(project);
        return true;
    }

    public async Task<bool> DeleteProjectAsync(Guid projectGuid)
    {
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Guid == projectGuid && p.ValidUntil == null);
        if (project != null)
        {
            project.ValidUntil = _clock.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

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
