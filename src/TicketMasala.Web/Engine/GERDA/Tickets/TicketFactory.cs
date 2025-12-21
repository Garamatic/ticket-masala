using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Repositories;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities; // ApplicationUser, Employee

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// Factory for creating Ticket objects with consistent defaults.
/// Implements the Factory pattern for complex object creation.
/// </summary>
public class TicketFactory : ITicketFactory
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<TicketFactory> _logger;

    public TicketFactory(
        IUserRepository userRepository,
        ILogger<TicketFactory> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Create a ticket with sensible defaults.
    /// Note: Description, Title, DomainId are required and must be set after calling this method.
    /// </summary>
    public Ticket CreateWithDefaults()
    {

        return new Ticket
        {
            Guid = Guid.NewGuid(),
            Description = string.Empty, // Required, must be set by caller
            Title = string.Empty, // Required, must be set by caller
            DomainId = "IT", // Required, can be overridden by caller
            Status = "New", // Required
            TicketStatus = TicketMasala.Domain.Common.Status.Pending,
            CustomFieldsJson = "{}",
            CreationDate = DateTime.UtcNow,
            CompletionTarget = DateTime.UtcNow.AddDays(14),
            PriorityScore = 50,
            EstimatedEffortPoints = 0,
            Comments = new List<TicketComment>(),
            SubTickets = new List<Ticket>(),
        };
    }

    /// <summary>
    /// Create a ticket from form submission data
    /// </summary>
    public async Task<Ticket> CreateTicketAsync(
        string title,
        string description,
        ApplicationUser customer,
        Priority priority = Priority.Medium,
        Employee? responsible = null,
        Guid? projectGuid = null,
        DateTime? completionTarget = null)
    {
        var ticket = new Ticket
        {
            Guid = Guid.NewGuid(),
            Title = title,
            Description = description,
            DomainId = "IT",
            Status = responsible != null ? "Assigned" : "New",
            TicketStatus = responsible != null ? TicketMasala.Domain.Common.Status.Assigned : TicketMasala.Domain.Common.Status.Pending,
            CustomFieldsJson = "{}",
            CreatorGuid = Guid.Parse(customer.Id),
            CreationDate = DateTime.UtcNow,
            CompletionTarget = completionTarget ?? DateTime.UtcNow.AddDays(14),
            PriorityScore = 50,
            EstimatedEffortPoints = 0,
            Comments = new List<TicketComment>(),
            SubTickets = new List<Ticket>(),
            // V2 Grouping: Compute Hash
            ContentHash = TicketHasher.ComputeContentHash(description, customer.Id)
        };

        if (responsible != null)
        {
            ticket.Responsible = responsible;
            ticket.ResponsibleId = responsible.Id;
        }

        if (projectGuid.HasValue)
        {
            ticket.ProjectGuid = projectGuid.Value;
        }

        _logger.LogDebug("Created ticket from form: {Description} (Hash: {Hash})",
            description?.Substring(0, Math.Min(50, description?.Length ?? 0)),
            ticket.ContentHash);

        return ticket;
    }

    /// <summary>
    /// Create a ticket from email ingestion
    /// </summary>
    public async Task<Ticket> CreateFromEmailAsync(
        string subject,
        string body,
        string senderEmail)
    {
        // Try to find customer by email
        var customer = await _userRepository.GetUserByEmailAsync(senderEmail);

        // Build description from email
        var description = $"[Email] {subject}\n\n{body}";

        var ticket = new Ticket
        {
            Guid = Guid.NewGuid(),
            Title = subject,
            Description = description,
            DomainId = "IT",
            Status = "New",
            GerdaTags = "Email-Ingested",
            CreationDate = DateTime.UtcNow,
            CompletionTarget = DateTime.UtcNow.AddDays(14),
            PriorityScore = 50,
            EstimatedEffortPoints = 0,
            Comments = new List<TicketComment>(),
            SubTickets = new List<Ticket>(),
            // V2 Grouping: Compute Hash
            ContentHash = TicketHasher.ComputeContentHash(description, customer?.Id ?? senderEmail)
        };

        if (customer != null)
        {
            ticket.CreatorGuid = Guid.Parse(customer.Id);
        }

        _logger.LogInformation("Created ticket from email: {Subject} from {Sender} (Hash: {Hash})",
            subject, senderEmail, ticket.ContentHash);

        return ticket;
    }
}
