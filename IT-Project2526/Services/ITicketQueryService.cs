using IT_Project2526.Models;
using IT_Project2526.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IT_Project2526.Services;

/// <summary>
/// Query service for ticket read operations.
/// Part of CQRS-lite pattern splitting from TicketService.
/// </summary>
public interface ITicketQueryService
{
    /// <summary>
    /// Get all tickets with customer and responsible information
    /// </summary>
    Task<IEnumerable<TicketSearchViewModel>> GetAllTicketsAsync();

    /// <summary>
    /// Get detailed ticket information with GERDA insights
    /// </summary>
    Task<TicketDetailsViewModel?> GetTicketDetailsAsync(Guid ticketGuid);

    /// <summary>
    /// Get ticket for editing with relations
    /// </summary>
    Task<Ticket?> GetTicketForEditAsync(Guid ticketGuid);

    /// <summary>
    /// Search tickets with filters
    /// </summary>
    Task<IEnumerable<TicketSearchViewModel>> SearchTicketsAsync(
        string? query,
        Status? status,
        string? responsibleId,
        Guid? projectGuid,
        DateTime? fromDate,
        DateTime? toDate);

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
    /// Get all users for edit form dropdown
    /// </summary>
    Task<List<SelectListItem>> GetAllUsersSelectListAsync();

    /// <summary>
    /// Get employee by ID for GERDA recommendation details
    /// </summary>
    Task<Employee?> GetEmployeeByIdAsync(string employeeId);

    /// <summary>
    /// Calculate current workload for an employee
    /// </summary>
    Task<int> GetEmployeeCurrentWorkloadAsync(string employeeId);
}
