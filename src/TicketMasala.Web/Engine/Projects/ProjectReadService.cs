using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Data;
using TicketMasala.Web.Repositories;
using TicketMasala.Web.ViewModels.Projects;
using TicketMasala.Web.ViewModels.Tickets;

namespace TicketMasala.Web.Engine.Projects;

public class ProjectReadService : IProjectReadService
{
    private readonly MasalaDbContext _context;
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<ProjectReadService> _logger;

    public ProjectReadService(
        MasalaDbContext context,
        IProjectRepository projectRepository,
        ILogger<ProjectReadService> logger)
    {
        _context = context;
        _projectRepository = projectRepository;
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
                CustomerId = project.CustomerId,
                CompletionTarget = project.CompletionTarget,
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
            ProjectType = project.ProjectType,
            ProjectComment = project.Notes,
            SelectedProjectManagerId = project.ProjectManagerId,
            CustomerList = (await GetCustomerSelectListAsync(project.Customer?.Id)).ToList(),
            ProjectManagerList = (await GetEmployeeSelectListAsync(project.ProjectManagerId)).Items.Cast<SelectListItem>().ToList()
        };
    }

    public async Task<IEnumerable<SelectListItem>> GetCustomerSelectListAsync(string? selectedCustomerId = null)
    {
        var customers = await _context.Users.ToListAsync();
        return customers.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = $"{c.FirstName} {c.LastName}",
            Selected = selectedCustomerId != null && c.Id == selectedCustomerId
        });
    }

    public async Task<IEnumerable<SelectListItem>> GetStakeholderSelectListAsync()
    {
        var employees = await _context.Users.OfType<Employee>().ToListAsync();
        return employees.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = $"{c.FirstName} {c.LastName}"
        });
    }

    public async Task<IEnumerable<SelectListItem>> GetTemplateSelectListAsync()
    {
        var templates = await _context.ProjectTemplates.ToListAsync();
        return templates.Select(t => new SelectListItem
        {
            Value = t.Guid.ToString(),
            Text = t.Name
        });
    }

    public async Task<SelectList> GetEmployeeSelectListAsync(string? selectedId = null)
    {
        var employees = await _context.Users.OfType<Employee>()
            .Where(e => e.Level == EmployeeType.ProjectManager)
            .ToListAsync();
        var items = employees.Select(e => new SelectListItem
        {
            Value = e.Id,
            Text = $"{e.FirstName} {e.LastName}",
            Selected = selectedId != null && e.Id == selectedId
        }).ToList();

        return new SelectList(items, "Value", "Text", selectedId);
    }

    public async Task<IEnumerable<ProjectTicketViewModel>> GetProjectsByCustomerAsync(string customerId)
    {
        var projects = await _context.Projects
            .AsNoTracking()
            .Include(p => p.Tasks.Where(t => t.ValidUntil == null))
                .ThenInclude(t => t.Responsible)
            .Include(p => p.Tasks.Where(t => t.ValidUntil == null))
                .ThenInclude(t => t.Customer)
            .Include(p => p.Customer)
            .Include(p => p.ProjectManager)
            .Where(p => p.CustomerId == customerId && p.ValidUntil == null)
            .Select(p => new ProjectTicketViewModel
            {
                ProjectDetails = new ProjectViewModel
                {
                    Guid = p.Guid,
                    Name = p.Name,
                    Description = p.Description,
                    Status = p.Status,
                    ProjectManagerName = p.ProjectManager != null
                        ? $"{p.ProjectManager.FirstName} {p.ProjectManager.LastName}"
                        : "Not Assigned",
                    TicketCount = p.Tasks.Count
                },
                Tasks = p.Tasks.Select(t => new TicketViewModel
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
            })
            .ToListAsync();

        return projects;
    }

    public async Task<IEnumerable<ProjectTicketViewModel>> SearchProjectsAsync(string query)
    {
        var projects = await _context.Projects
            .AsNoTracking()
            .Include(p => p.Tasks.Where(t => t.ValidUntil == null))
                .ThenInclude(t => t.Responsible)
            .Include(p => p.Tasks.Where(t => t.ValidUntil == null))
                .ThenInclude(t => t.Customer)
            .Include(p => p.Customer)
            .Include(p => p.ProjectManager)
            .Where(p => p.ValidUntil == null &&
                (p.Name.Contains(query) || p.Description.Contains(query)))
            .Select(p => new ProjectTicketViewModel
            {
                ProjectDetails = new ProjectViewModel
                {
                    Guid = p.Guid,
                    Name = p.Name,
                    Description = p.Description,
                    Status = p.Status,
                    ProjectManagerName = p.ProjectManager != null
                        ? $"{p.ProjectManager.FirstName} {p.ProjectManager.LastName}"
                        : "Not Assigned",
                    TicketCount = p.Tasks.Count
                },
                Tasks = p.Tasks.Select(t => new TicketViewModel
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
            })
            .ToListAsync();

        return projects;
    }

    public async Task<ProjectStatisticsViewModel> GetProjectStatisticsAsync(string customerId)
    {
        var projects = await _context.Projects
           .AsNoTracking()
           .Include(p => p.Tasks)
           .Where(p => p.CustomerId == customerId && p.ValidUntil == null)
           .ToListAsync();

        return new ProjectStatisticsViewModel
        {
            TotalProjects = projects.Count,
            ActiveProjects = projects.Count(p => p.Status == Status.InProgress),
            CompletedProjects = projects.Count(p => p.Status == Status.Completed),
            PendingProjects = projects.Count(p => p.Status == Status.Pending),
            TotalTasks = projects.Sum(p => p.Tasks.Count),
            CompletedTasks = projects.Sum(p => p.Tasks.Count(t => t.TicketStatus == Status.Completed))
        };
    }

    public async Task<Guid?> GetProjectIdForTicketAsync(Guid ticketId)
    {
        var ticket = await _context.Tickets
            .AsNoTracking()
            .Where(t => t.Guid == ticketId && t.ProjectGuid.HasValue)
            .Select(t => t.ProjectGuid)
            .FirstOrDefaultAsync();

        return ticket;
    }

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

        var employees = await _context.Users.OfType<Employee>()
            .Where(e => e.Level == EmployeeType.ProjectManager)
            .ToListAsync();
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
            SelectedPMId = recommendedPMId,
            TargetCompletionDate = DateTime.UtcNow.AddDays(30),
            TemplateList = new SelectList(await GetTemplateSelectListAsync(), "Value", "Text"),
            ProjectManagerList = await GetEmployeeSelectListAsync(recommendedPMId)
        };
    }
}
