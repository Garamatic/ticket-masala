using TicketMasala.Web;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TicketMasala.Web.Controllers;

// Partial class: Batch operations, export, and time logging
public partial class TicketController
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Constants.RoleEmployee + "," + Constants.RoleAdmin)]
    public async Task<IActionResult> BatchAssign(List<Guid> ticketIds, string agentId)
    {
        if (ticketIds != null && ticketIds.Any() && !string.IsNullOrEmpty(agentId))
        {
            await _ticketService.BatchAssignToAgentAsync(ticketIds, agentId);
            TempData["Success"] = $"{ticketIds.Count} ticket(s) assigned successfully.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Constants.RoleEmployee + "," + Constants.RoleAdmin)]
    public async Task<IActionResult> BatchStatus(List<Guid> ticketIds, Status status)
    {
        if (ticketIds != null && ticketIds.Any())
        {
            await _ticketService.BatchUpdateStatusAsync(ticketIds, status);
            TempData["Success"] = $"{ticketIds.Count} ticket(s) updated successfully.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportTickets(ViewModels.Tickets.TicketSearchViewModel searchModel)
    {
        try
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isCustomer = User.IsInRole(Constants.RoleCustomer);

            if (isCustomer && !string.IsNullOrEmpty(userId))
            {
                searchModel.CustomerId = userId;
            }

            var result = await _ticketService.SearchTicketsAsync(searchModel);

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Guid,Description,Status,Customer,Responsible,CreationDate,CompletionTarget");

            foreach (var ticket in result.Results)
            {
                var customerName = ticket.Customer != null ? $"{ticket.Customer.FirstName} {ticket.Customer.LastName}" : "Unknown";
                var responsibleName = ticket.Responsible != null ? $"{ticket.Responsible.FirstName} {ticket.Responsible.LastName}" : "Not Assigned";
                csv.AppendLine($"{ticket.Guid},\"{ticket.Description}\",{ticket.TicketStatus},\"{customerName}\",\"{responsibleName}\",{ticket.CreationDate:yyyy-MM-dd},{ticket.CompletionTarget:yyyy-MM-dd}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"tickets-export-{DateTime.Now:yyyyMMdd}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting tickets");
            TempData["Error"] = "Failed to export tickets.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    [Authorize(Roles = Constants.RoleEmployee + "," + Constants.RoleAdmin)]
    public async Task<IActionResult> LogTime(Guid id)
    {
        var ticket = await _ticketService.GetTicketForEditAsync(id);
        if (ticket == null) return NotFound();

        ViewBag.TicketGuid = id;
        ViewBag.TicketDescription = ticket.Description;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Constants.RoleEmployee + "," + Constants.RoleAdmin)]
    public async Task<IActionResult> LogTime(Guid id, double hours, DateTime date, string description)
    {
        if (hours <= 0 || hours > 24)
        {
            ModelState.AddModelError("hours", "Hours must be between 0.1 and 24");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            ModelState.AddModelError("description", "Description is required");
        }

        if (!ModelState.IsValid)
        {
            var ticket = await _ticketService.GetTicketForEditAsync(id);
            ViewBag.TicketGuid = id;
            ViewBag.TicketDescription = ticket?.Description;
            return View();
        }

        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        try
        {
            await _ticketService.LogTimeAsync(id, userId, hours, date, description);
            TempData["Success"] = $"Successfully logged {hours} hours on this ticket.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging time for ticket {TicketId}", id);
            TempData["Error"] = "Failed to log time. Please try again.";
        }

        return RedirectToAction(nameof(Detail), new { id });
    }
}
