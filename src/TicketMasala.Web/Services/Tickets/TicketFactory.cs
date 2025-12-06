using TicketMasala.Web.Models;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Services.Tickets;

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
    /// Note: Description and Customer are required and must be set after calling this method.
    /// </summary>
    public Ticket CreateWithDefaults()
    {
        return new Ticket
        {
            Guid = Guid.NewGuid(),
            Description = string.Empty, // Required, must be set by caller
            Customer = null!, // Required, must be set by caller
            CreationDate = DateTime.UtcNow,
            TicketStatus = Status.Pending,
            TicketType = TicketType.ProjectRequest,
            CompletionTarget = DateTime.UtcNow.AddDays(14),
            PriorityScore = 50,
            EstimatedEffortPoints = 0,
            Comments = new List<TicketComment>(),
            SubTickets = new List<Ticket>(),
            // QualityReviews removed
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
            Description = description,
            Customer = customer,
            CustomerId = customer.Id,
            CreatorGuid = Guid.Parse(customer.Id),
            CreationDate = DateTime.UtcNow,
            TicketStatus = responsible != null ? Status.Assigned : Status.Pending,
            TicketType = TicketType.ProjectRequest,
            CompletionTarget = completionTarget ?? DateTime.UtcNow.AddDays(14),
            PriorityScore = 50,
            EstimatedEffortPoints = 0,
            Comments = new List<TicketComment>(),
            SubTickets = new List<Ticket>(),
            // QualityReviews removed
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
        
        _logger.LogDebug("Created ticket from form: {Description}", description?.Substring(0, Math.Min(50, description?.Length ?? 0)));
        
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
            Description = description,
            Customer = customer!, // May be null if customer not found
            TicketType = TicketType.ServiceRequest, // Email tickets are service requests
            GerdaTags = "Email-Ingested",
            CreationDate = DateTime.UtcNow,
            TicketStatus = Status.Pending,
            CompletionTarget = DateTime.UtcNow.AddDays(14),
            PriorityScore = 50,
            EstimatedEffortPoints = 0,
            Comments = new List<TicketComment>(),
            SubTickets = new List<Ticket>(),
            // QualityReviews removed
        };
        
        if (customer != null)
        {
            ticket.CustomerId = customer.Id;
            ticket.CreatorGuid = Guid.Parse(customer.Id);
        }
        
        _logger.LogInformation("Created ticket from email: {Subject} from {Sender}", subject, senderEmail);
        
        return ticket;
    }


}
