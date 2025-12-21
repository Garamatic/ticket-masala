using System.Text.Json;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.ViewModels.Api;

namespace TicketMasala.Web.Extensions;

/// <summary>
/// Extension methods for mapping between domain entities and API DTOs.
/// Provides domain-agnostic mapping for consistent API interfaces.
/// </summary>
public static class DtoMappingExtensions
{
    /// <summary>
    /// Maps a Ticket entity to a WorkItemDto for API responses.
    /// </summary>
    /// <param name="ticket">The ticket entity to map</param>
    /// <returns>A WorkItemDto with mapped values</returns>
    public static WorkItemDto ToWorkItemDto(this Ticket ticket)
    {
        if (ticket == null)
            throw new ArgumentNullException(nameof(ticket));

        // Parse custom fields JSON safely
        Dictionary<string, object>? customFields = null;
        if (!string.IsNullOrEmpty(ticket.CustomFieldsJson) && ticket.CustomFieldsJson != "{}")
        {
            try
            {
                customFields = JsonSerializer.Deserialize<Dictionary<string, object>>(ticket.CustomFieldsJson);
            }
            catch (JsonException)
            {
                // If JSON parsing fails, leave customFields as null
                customFields = null;
            }
        }

        return new WorkItemDto
        {
            Id = ticket.Guid,
            Title = ticket.Title,
            Description = ticket.Description,
            Status = ticket.Status,
            AssignedHandlerId = ticket.ResponsibleId,
            ContainerId = ticket.ProjectGuid,
            DomainId = ticket.DomainId,
            TypeCode = ticket.WorkItemTypeCode,
            CreatedAt = ticket.CreationDate,
            CompletedAt = ticket.CompletionDate,
            CompletionTarget = ticket.CompletionTarget,
            CustomFields = customFields,
            EstimatedEffortPoints = ticket.EstimatedEffortPoints > 0 ? ticket.EstimatedEffortPoints : null,
            PriorityScore = ticket.PriorityScore > 0 ? ticket.PriorityScore : null,
            CustomerId = ticket.CustomerId
        };
    }

    /// <summary>
    /// Maps a Project entity to a WorkContainerDto for API responses.
    /// </summary>
    /// <param name="project">The project entity to map</param>
    /// <param name="workItemCount">Optional count of work items in this container</param>
    /// <returns>A WorkContainerDto with mapped values</returns>
    public static WorkContainerDto ToWorkContainerDto(this Project project, int workItemCount = 0)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        return new WorkContainerDto
        {
            Id = project.Guid,
            Name = project.Name,
            Description = project.Description,
            Status = project.Status.ToString(),
            ManagerId = project.ProjectManagerId,
            CreatedAt = project.CreationDate,
            CompletionTarget = project.CompletionTarget,
            CompletedAt = project.CompletionDate,
            WorkItemCount = workItemCount,
            ProjectType = project.ProjectType,
            Notes = project.Notes,
            CustomerId = project.CustomerId,
            CustomerIds = project.CustomerIds ?? new List<string>(),
            AiRoadmap = project.ProjectAiRoadmap
        };
    }

    /// <summary>
    /// Maps an ApplicationUser entity to a WorkHandlerDto for API responses.
    /// </summary>
    /// <param name="user">The user entity to map</param>
    /// <returns>A WorkHandlerDto with mapped values</returns>
    public static WorkHandlerDto ToWorkHandlerDto(this ApplicationUser user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        var dto = new WorkHandlerDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email ?? string.Empty,
            Phone = user.Phone,
            UserName = user.UserName,
            IsActive = !user.LockoutEnabled || user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow
        };

        // If the user is an Employee, map additional fields
        if (user is Employee employee)
        {
            dto.Team = employee.Team;
            dto.Level = employee.Level.ToString();
            dto.Language = employee.Language;
            dto.MaxCapacityPoints = employee.MaxCapacityPoints;
            dto.Region = employee.Region;
            dto.ProfilePicturePath = employee.ProfilePicturePath;

            // Parse specializations JSON safely
            if (!string.IsNullOrEmpty(employee.Specializations))
            {
                try
                {
                    var specializations = JsonSerializer.Deserialize<string[]>(employee.Specializations);
                    dto.Specializations = specializations?.ToList() ?? new List<string>();
                }
                catch (JsonException)
                {
                    // If JSON parsing fails, leave as empty list
                    dto.Specializations = new List<string>();
                }
            }
        }

        return dto;
    }

    /// <summary>
    /// Maps a WorkItemDto back to a Ticket entity for updates.
    /// Note: This is a partial mapping for update scenarios - not all fields are mapped.
    /// </summary>
    /// <param name="dto">The WorkItemDto to map from</param>
    /// <param name="existingTicket">Optional existing ticket to update</param>
    /// <returns>A Ticket entity with mapped values</returns>
    public static Ticket ToTicket(this WorkItemDto dto, Ticket? existingTicket = null)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var ticket = existingTicket ?? new Ticket();

        // Map basic fields
        ticket.Title = dto.Title;
        ticket.Description = dto.Description;
        ticket.Status = dto.Status;
        ticket.ResponsibleId = dto.AssignedHandlerId;
        ticket.ProjectGuid = dto.ContainerId;
        ticket.DomainId = dto.DomainId;
        ticket.WorkItemTypeCode = dto.TypeCode;
        ticket.CompletionTarget = dto.CompletionTarget;
        ticket.CustomerId = dto.CustomerId;

        // Map AI fields if provided
        if (dto.EstimatedEffortPoints.HasValue)
            ticket.EstimatedEffortPoints = dto.EstimatedEffortPoints.Value;
        if (dto.PriorityScore.HasValue)
            ticket.PriorityScore = dto.PriorityScore.Value;

        // Serialize custom fields to JSON
        if (dto.CustomFields != null && dto.CustomFields.Any())
        {
            try
            {
                ticket.CustomFieldsJson = JsonSerializer.Serialize(dto.CustomFields);
            }
            catch (JsonException)
            {
                // If serialization fails, keep existing or default value
                ticket.CustomFieldsJson = existingTicket?.CustomFieldsJson ?? "{}";
            }
        }
        else
        {
            ticket.CustomFieldsJson = "{}";
        }

        return ticket;
    }

    /// <summary>
    /// Maps a WorkContainerDto back to a Project entity for updates.
    /// Note: This is a partial mapping for update scenarios - not all fields are mapped.
    /// </summary>
    /// <param name="dto">The WorkContainerDto to map from</param>
    /// <param name="existingProject">Optional existing project to update</param>
    /// <returns>A Project entity with mapped values</returns>
    public static Project ToProject(this WorkContainerDto dto, Project? existingProject = null)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var project = existingProject ?? new Project
        {
            Name = string.Empty,
            Description = string.Empty
        };

        // Map basic fields
        project.Name = dto.Name;
        project.Description = dto.Description ?? string.Empty;
        project.ProjectManagerId = dto.ManagerId;
        project.CompletionTarget = dto.CompletionTarget;
        project.ProjectType = dto.ProjectType;
        project.Notes = dto.Notes;
        project.CustomerId = dto.CustomerId;
        project.CustomerIds = dto.CustomerIds ?? new List<string>();
        project.ProjectAiRoadmap = dto.AiRoadmap;

        // Parse status enum safely
        if (Enum.TryParse<Domain.Common.Status>(dto.Status, true, out var status))
        {
            project.Status = status;
        }

        return project;
    }
}