using IT_Project2526.Models;

namespace IT_Project2526.Services;

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
    Task<Ticket> CreateFromFormAsync(
        string description,
        Customer customer,
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
