using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Data;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.ViewModels.Projects;
using TicketMasala.Web.AI;
using TicketMasala.Web.Observers;

namespace TicketMasala.Web.Engine.Projects;

public class ProjectWorkflowService : IProjectWorkflowService
{
    private readonly MasalaDbContext _context;
    private readonly IProjectRepository _projectRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEnumerable<IProjectObserver> _observers;
    private readonly IOpenAiService _openAiService;
    private readonly IProjectTemplateService _templateService;
    private readonly ILogger<ProjectWorkflowService> _logger;

    public ProjectWorkflowService(
        MasalaDbContext context,
        IProjectRepository projectRepository,
        UserManager<ApplicationUser> userManager,
        IEnumerable<IProjectObserver> observers,
        IOpenAiService openAiService,
        IProjectTemplateService templateService,
        ILogger<ProjectWorkflowService> logger)
    {
        _context = context;
        _projectRepository = projectRepository;
        _userManager = userManager;
        _observers = observers;
        _openAiService = openAiService;
        _templateService = templateService;
        _logger = logger;
    }

    public async Task<Project> CreateProjectAsync(NewProject viewModel, string userId)
    {
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

            await _userManager.CreateAsync(customer);
            await _userManager.AddToRoleAsync(customer, Constants.RoleCustomer);
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
            roadmap = await _openAiService.GetResponseAsync(OpenAIPrompts.Steps, viewModel.Description);
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
            CreatorGuid = Guid.Parse(userId),
            ProjectAiRoadmap = roadmap,
        };

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
            roadmap = await _openAiService.GetResponseAsync(OpenAIPrompts.Steps, ticket.Description);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate AI roadmap for project from ticket");
        }

        var project = new Project
        {
            Name = viewModel.ProjectName,
            Description = viewModel.ProjectDescription ?? ticket.Description,
            Status = Status.Pending,
            Customer = customer,
            CustomerId = customer?.Id,
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
            }
        }
        else
        {
            project.ProjectManager = null; // Unassign if cleared
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

        _context.Projects.Update(project);
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
            project.CompletionDate = DateTime.UtcNow;
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
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Guid == projectGuid);
        if (project != null)
        {
            project.ValidUntil = DateTime.UtcNow;
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
