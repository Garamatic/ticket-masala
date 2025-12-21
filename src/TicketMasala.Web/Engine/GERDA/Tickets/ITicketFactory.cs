using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities; // ApplicationUser, Employee

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// Factory for creating Ticket objects with consistent defaults.
/// Implements the Factory pattern for complex object creation.
/// </summary>
public interface ITicketFactory
{
    /// <summary>
    /// Create a ticket with sensible defaults
    /// </summary>
    Ticket CreateWithDefaults();

    /// <summary>
    /// Create a ticket from form submission data
    /// </summary>
    Task<Ticket> CreateTicketAsync(
        string title,
        string description,
        ApplicationUser customer,
        Priority priority = Priority.Medium,
        Employee? responsible = null,
        Guid? projectGuid = null,
        DateTime? completionTarget = null);

    /// <summary>
    /// Create a ticket from email ingestion
    /// </summary>
    Task<Ticket> CreateFromEmailAsync(
        string subject,
        string body,
        string senderEmail);

}
