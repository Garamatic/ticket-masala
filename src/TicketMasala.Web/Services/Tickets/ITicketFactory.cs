using TicketMasala.Web.Models;

namespace TicketMasala.Web.Services.Tickets;

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
        Priority priority = Priority.Medium);

    /// <summary>
    /// Create a ticket from email ingestion
    /// </summary>
    Task<Ticket> CreateFromEmailAsync(
        string subject,
        string body,
        string senderEmail);

}
