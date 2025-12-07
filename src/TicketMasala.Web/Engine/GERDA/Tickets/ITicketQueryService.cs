using TicketMasala.Web.Models;
using TicketMasala.Web.ViewModels.Tickets;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// Query service for ticket read operations.
/// Follows CQRS pattern - separates read concerns from write concerns.
/// </summary>
public interface ITicketQueryService
{
    /// <summary>
    /// Get customer dropdown list
    /// </summary>
    Task<List<SelectListItem>> GetCustomerSelectListAsync();

    /// <summary>
    /// Get employee dropdown list
    /// </summary>
    Task<List<SelectListItem>> GetEmployeeSelectListAsync();

    /// <summary>
    /// Get project dropdown list
    /// </summary>
    Task<List<SelectListItem>> GetProjectSelectListAsync();

    /// <summary>
    /// Get current user's department ID
    /// </summary>
    Task<Guid?> GetCurrentUserDepartmentIdAsync();

    /// <summary>
    /// Get all tickets with customer and responsible information
    /// </summary>
    Task<IEnumerable<TicketViewModel>> GetAllTicketsAsync();

    /// <summary>
    /// Get detailed ticket information with GERDA insights
    /// </summary>
    Task<TicketDetailsViewModel?> GetTicketDetailsAsync(Guid ticketGuid);

    /// <summary>
    /// Get employee by ID for GERDA recommendation details
    /// </summary>
    Task<Employee?> GetEmployeeByIdAsync(string agentId);

    /// <summary>
    /// Calculate current workload for an employee
    /// </summary>
    Task<int> GetEmployeeCurrentWorkloadAsync(string agentId);

    /// <summary>
    /// Get ticket for editing with relations
    /// </summary>
    Task<Ticket?> GetTicketForEditAsync(Guid ticketGuid);

    /// <summary>
    /// Get all users for edit form dropdown
    /// </summary>
    Task<List<SelectListItem>> GetAllUsersSelectListAsync();

    /// <summary>
    /// Search tickets with filters and pagination
    /// </summary>
    Task<TicketSearchViewModel> SearchTicketsAsync(TicketSearchViewModel searchModel);

}
