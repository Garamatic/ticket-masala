using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Enums;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Engine.GERDA.Tickets;

/// <summary>
/// Factory for creating Ticket objects with consistent defaults.
/// Implements the Factory pattern for complex object creation.
/// </summary>
public class TicketFactory : ITicketFactory
{
    private readonly IUserRepository _userRepository;
    private readonly ISystemClock _clock;
    private readonly ILogger<TicketFactory> _logger;

    public TicketFactory(
        IUserRepository userRepository,
        ISystemClock clock,
        ILogger<TicketFactory> logger)
    {
        _userRepository = userRepository;
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger;
    }

    /// <summary>
    /// Create a ticket with sensible defaults.
    /// Note: Description, Title, DomainId are required and must be set after calling this method.
    /// </summary>
    public Ticket CreateWithDefaults()
    {

        var ticket = new Ticket
        {
            Description = string.Empty, // Required, must be set by caller
            Title = string.Empty, // Required, must be set by caller
            DomainId = "IT", // Required, can be overridden by caller
            TicketStatus = TicketMasala.Domain.Common.Status.Pending,
            CustomFieldsJson = "{}",
            CompletionTarget = _clock.UtcNow.AddDays(14),
            PriorityScore = 50,
            EstimatedEffortPoints = 0,
        };
        ticket.SyncStatus();
        return ticket;
    }

    /// <summary>
    /// Create a ticket from form submission data
    /// </summary>
    public Task<Ticket> CreateTicketAsync(
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
            Title = title,
            Description = description,
            DomainId = "IT",
            TicketStatus = responsible != null ? TicketMasala.Domain.Common.Status.Assigned : TicketMasala.Domain.Common.Status.Pending,
            CustomFieldsJson = "{}",
            CreatorGuid = Guid.Parse(customer.Id),
            CompletionTarget = completionTarget ?? _clock.UtcNow.AddDays(14),
            PriorityScore = 50,
            EstimatedEffortPoints = 0,
            // V2 Grouping: Compute Hash
            ContentHash = TicketHasher.ComputeContentHash(description, customer.Id)
        };
        ticket.SyncStatus();

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

        return Task.FromResult(ticket);
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
            Title = subject,
            Description = description,
            DomainId = "IT",
            GerdaTags = "Email-Ingested",
            CompletionTarget = _clock.UtcNow.AddDays(14),
            PriorityScore = 50,
            EstimatedEffortPoints = 0,
            // V2 Grouping: Compute Hash
            ContentHash = TicketHasher.ComputeContentHash(description, customer?.Id ?? senderEmail)
        };
        ticket.SyncStatus();

        if (customer != null)
        {
            ticket.CreatorGuid = Guid.Parse(customer.Id);
        }

        _logger.LogInformation("Created ticket from email: {Subject} from {Sender} (Hash: {Hash})",
            subject, senderEmail, ticket.ContentHash);

        return ticket;
    }
}
