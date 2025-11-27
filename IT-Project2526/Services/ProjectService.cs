using IT_Project2526.Models;
using IT_Project2526.Repositories;
using IT_Project2526.ViewModels;
using IT_Project2526.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace IT_Project2526.Services
{
    /// <summary>
    /// Service implementation for project business logic with caching
    /// </summary>
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<ProjectService> _logger;
        private readonly IMemoryCache _cache;
        
        // Cache keys
        private const string ALL_PROJECTS_CACHE_KEY = "all_projects";
        private const string PROJECT_CACHE_KEY_PREFIX = "project_";
        private const string CUSTOMER_PROJECTS_KEY_PREFIX = "customer_projects_";
        private const string MANAGER_PROJECTS_KEY_PREFIX = "manager_projects_";
        
        // Cache duration
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public ProjectService(
            IProjectRepository projectRepository,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ILogger<ProjectService> logger,
            IMemoryCache cache)
        {
            _projectRepository = projectRepository;
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
            _cache = cache;
        }

        public async Task<IEnumerable<ProjectTicketViewModel>> GetAllProjectsAsync()
        {
            try
            {
                return await _cache.GetOrCreateAsync(ALL_PROJECTS_CACHE_KEY, async entry =>
                {
                    entry.SlidingExpiration = CacheDuration;
                    _logger.LogInformation("Cache miss for all projects - fetching from database");
                    
                    var projects = await _projectRepository.GetAllWithDetailsAsync();
                    return MapToViewModels(projects);
                }) ?? Enumerable.Empty<ProjectTicketViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all projects");
                throw;
            }
        }

        public async Task<ProjectTicketViewModel?> GetProjectByIdAsync(Guid id)
        {
            try
            {
                var cacheKey = $"{PROJECT_CACHE_KEY_PREFIX}{id}";
                
                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.SlidingExpiration = CacheDuration;
                    _logger.LogInformation("Cache miss for project {ProjectId} - fetching from database", id);
                    
                    var project = await _projectRepository.GetByIdWithDetailsAsync(id);
                    return project == null ? null : MapToViewModel(project);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project {ProjectId}", id);
                throw;
            }
        }

        public async Task<Guid> CreateProjectAsync(NewProject model, string currentUserId)
        {
            try
            {
                _logger.LogInformation("Creating new project: {ProjectName} by user {UserId}", 
                    model.Name, currentUserId);

                Customer customer;

                if (model.IsNewCustomer)
                {
                    customer = await CreateNewCustomerAsync(model);
                }
                else
                {
                    customer = await _userManager.FindByIdAsync(model.SelectedCustomerId!) as Customer
                        ?? throw new InvalidOperationException($"Customer not found: {model.SelectedCustomerId}");
                }

                var project = new Project
                {
                    Name = model.Name,
                    Description = model.Description,
                    Status = Status.Pending,
                    CustomerId = customer.Id,
                    Customer = customer,
                    CreatorGuid = Guid.TryParse(currentUserId, out var creatorGuid) ? creatorGuid : null
                };

                await _projectRepository.AddAsync(project);
                await _projectRepository.SaveChangesAsync();

                _logger.LogInformation("Project created successfully: {ProjectId} - {ProjectName}", 
                    project.Guid, project.Name);

                // Invalidate cache
                InvalidateProjectCaches(customer.Id);

                // Send notification email
                await _emailService.SendProjectAssignmentEmailAsync(customer.Email!, project.Name);

                return project.Guid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project: {ProjectName}", model.Name);
                throw;
            }
        }

        public async Task UpdateProjectAsync(Guid id, NewProject model)
        {
            try
            {
                var project = await _projectRepository.GetByIdAsync(id)
                    ?? throw new InvalidOperationException($"Project not found: {id}");

                project.Name = model.Name;
                project.Description = model.Description;

                await _projectRepository.UpdateAsync(project);
                await _projectRepository.SaveChangesAsync();

                // Invalidate cache
                InvalidateProjectCache(id, project.CustomerId);

                _logger.LogInformation("Project updated: {ProjectId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project {ProjectId}", id);
                throw;
            }
        }

        public async Task DeleteProjectAsync(Guid id)
        {
            try
            {
                var project = await _projectRepository.GetByIdAsync(id);
                await _projectRepository.DeleteAsync(id);
                await _projectRepository.SaveChangesAsync();

                // Invalidate cache
                if (project != null)
                {
                    InvalidateProjectCache(id, project.CustomerId);
                }

                _logger.LogInformation("Project soft deleted: {ProjectId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project {ProjectId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<ProjectTicketViewModel>> GetCustomerProjectsAsync(string customerId)
        {
            try
            {
                var cacheKey = $"{CUSTOMER_PROJECTS_KEY_PREFIX}{customerId}";
                
                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.SlidingExpiration = CacheDuration;
                    _logger.LogInformation("Cache miss for customer {CustomerId} projects", customerId);
                    
                    var projects = await _projectRepository.GetByCustomerIdAsync(customerId);
                    return MapToViewModels(projects);
                }) ?? Enumerable.Empty<ProjectTicketViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving projects for customer {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<IEnumerable<ProjectTicketViewModel>> GetManagerProjectsAsync(string managerId)
        {
            try
            {
                var cacheKey = $"{MANAGER_PROJECTS_KEY_PREFIX}{managerId}";
                
                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.SlidingExpiration = CacheDuration;
                    _logger.LogInformation("Cache miss for manager {ManagerId} projects", managerId);
                    
                    var projects = await _projectRepository.GetByProjectManagerIdAsync(managerId);
                    return MapToViewModels(projects);
                }) ?? Enumerable.Empty<ProjectTicketViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving projects for manager {ManagerId}", managerId);
                throw;
            }
        }

        public async Task<IEnumerable<ProjectTicketViewModel>> SearchProjectsAsync(string searchTerm)
        {
            // Don't cache search results as they're dynamic
            try
            {
                var projects = await _projectRepository.SearchByNameAsync(searchTerm);
                return MapToViewModels(projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching projects with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task AssignProjectManagerAsync(Guid projectId, string managerId)
        {
            try
            {
                var project = await _projectRepository.GetByIdAsync(projectId)
                    ?? throw new InvalidOperationException($"Project not found: {projectId}");

                var manager = await _userManager.FindByIdAsync(managerId) as Employee
                    ?? throw new InvalidOperationException($"Manager not found: {managerId}");

                var oldManagerId = project.ProjectManagerId;

                project.ProjectManagerId = managerId;
                project.ProjectManager = manager;

                await _projectRepository.UpdateAsync(project);
                await _projectRepository.SaveChangesAsync();

                // Invalidate relevant caches
                InvalidateProjectCache(projectId, project.CustomerId);
                if (!string.IsNullOrEmpty(oldManagerId))
                {
                    _cache.Remove($"{MANAGER_PROJECTS_KEY_PREFIX}{oldManagerId}");
                }
                _cache.Remove($"{MANAGER_PROJECTS_KEY_PREFIX}{managerId}");

                _logger.LogInformation("Project manager assigned: Project {ProjectId}, Manager {ManagerId}", 
                    projectId, managerId);

                // Send notification
                await _emailService.SendProjectAssignmentEmailAsync(manager.Email!, project.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning manager to project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task UpdateProjectStatusAsync(Guid projectId, Status newStatus)
        {
            try
            {
                var project = await _projectRepository.GetByIdAsync(projectId)
                    ?? throw new InvalidOperationException($"Project not found: {projectId}");

                var oldStatus = project.Status;
                project.Status = newStatus;

                if (newStatus == Status.Completed)
                {
                    project.CompletionDate = DateTime.UtcNow;
                }

                await _projectRepository.UpdateAsync(project);
                await _projectRepository.SaveChangesAsync();

                // Invalidate cache
                InvalidateProjectCache(projectId, project.CustomerId);

                _logger.LogInformation("Project status updated: {ProjectId} from {OldStatus} to {NewStatus}", 
                    projectId, oldStatus, newStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project status {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<ProjectStatistics> GetCustomerStatisticsAsync(string customerId)
        {
            try
            {
                // Statistics change frequently, shorter cache
                var cacheKey = $"customer_stats_{customerId}";
                
                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.SlidingExpiration = TimeSpan.FromMinutes(2);
                    return await _projectRepository.GetCustomerStatisticsAsync(customerId);
                }) ?? new ProjectStatistics();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics for customer {CustomerId}", customerId);
                throw;
            }
        }

        // Private helper methods

        private void InvalidateProjectCache(Guid projectId, string? customerId)
        {
            _cache.Remove(ALL_PROJECTS_CACHE_KEY);
            _cache.Remove($"{PROJECT_CACHE_KEY_PREFIX}{projectId}");
            
            if (!string.IsNullOrEmpty(customerId))
            {
                _cache.Remove($"{CUSTOMER_PROJECTS_KEY_PREFIX}{customerId}");
                _cache.Remove($"customer_stats_{customerId}");
            }
            
            _logger.LogDebug("Invalidated cache for project {ProjectId}", projectId);
        }

        private void InvalidateProjectCaches(string? customerId)
        {
            _cache.Remove(ALL_PROJECTS_CACHE_KEY);
            
            if (!string.IsNullOrEmpty(customerId))
            {
                _cache.Remove($"{CUSTOMER_PROJECTS_KEY_PREFIX}{customerId}");
                _cache.Remove($"customer_stats_{customerId}");
            }
        }

        private async Task<Customer> CreateNewCustomerAsync(NewProject model)
        {
            var customer = new Customer
            {
                UserName = model.NewCustomerEmail,
                Email = model.NewCustomerEmail,
                FirstName = model.NewCustomerFirstName ?? string.Empty,
                LastName = model.NewCustomerLastName ?? string.Empty,
                Phone = model.NewCustomerPhone
            };

            var tempPassword = PasswordHelper.GenerateWelcomePassword();
            var result = await _userManager.CreateAsync(customer, tempPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create customer: {errors}");
            }

            await _userManager.AddToRoleAsync(customer, Constants.RoleCustomer);

            _logger.LogInformation("New customer created: {Email}", customer.Email);

            // Send welcome email
            await _emailService.SendWelcomeEmailAsync(customer.Email!, customer.FirstName, tempPassword);

            return customer;
        }

        private ProjectTicketViewModel MapToViewModel(Project project)
        {
            return new ProjectTicketViewModel
            {
                ProjectDetails = new ProjectViewModel
                {
                    Guid = project.Guid,
                    Name = project.Name,
                    Description = project.Description,
                    Status = project.Status,
                    ProjectManager = project.ProjectManager
                },
                Tasks = project.Tasks?.Select(t => new TicketViewModel
                {
                    Guid = t.Guid,
                    Description = t.Description,
                    Status = t.TicketStatus.ToString(),
                    ResponsibleName = t.Responsible?.Name,
                    CommentsCount = t.Comments?.Count ?? 0,
                    CompletionTarget = t.CompletionTarget
                }).ToList() ?? new List<TicketViewModel>()
            };
        }

        private IEnumerable<ProjectTicketViewModel> MapToViewModels(IEnumerable<Project> projects)
        {
            return projects.Select(MapToViewModel);
        }
    }
}
