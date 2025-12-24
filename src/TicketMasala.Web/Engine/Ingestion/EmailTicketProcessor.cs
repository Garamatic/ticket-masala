using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.GERDA.Estimating;
using TicketMasala.Web.Engine.GERDA.Sentiment;

namespace TicketMasala.Web.Engine.Ingestion;

public record EmailContent(string Subject, string Body, string From);

public interface IEmailTicketProcessor
{
    Task<Ticket> ProcessEmailAsync(EmailContent email, CancellationToken cancellationToken);
}

public class EmailTicketProcessor : IEmailTicketProcessor
{
    private readonly MasalaDbContext _dbContext;
    private readonly IEstimatingService _estimatingService;
    private readonly ILogger<EmailTicketProcessor> _logger;

    public EmailTicketProcessor(
        MasalaDbContext dbContext,
        IEstimatingService estimatingService,
        ILogger<EmailTicketProcessor> logger)
    {
        _dbContext = dbContext;
        _estimatingService = estimatingService;
        _logger = logger;
    }

    public async Task<Ticket> ProcessEmailAsync(EmailContent email, CancellationToken cancellationToken)
    {
        // 1. Analyze Sentiment / Urgency
        var (urgencyScore, sentimentLabel) = SimpleSentimentAnalyzer.Analyze(email.Subject, email.Body);

        // 2. Create Ticket
        var ticket = new Ticket
        {
            Guid = Guid.NewGuid(),
            Title = email.Subject ?? "(No Subject)",
            Description = email.Body ?? "(No Content)",
            TicketStatus = Status.Pending,
            TicketType = TicketType.Incident, // Default to Incident for emails
            DomainId = "IT", // Default domain
            CreationDate = DateTime.UtcNow,

            // AI Fields
            PriorityScore = urgencyScore,
            GerdaTags = $"Email-Ingested,Sentiment-{sentimentLabel}",

            // Meta
            CustomFieldsJson = "{}",
            Status = "New"
        };

        // Determine Creator (simple logic for now)
        // In real app, look up user by email.From

        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created Ticket {TicketGuid} for '{Subject}' (Priority: {Score:F1} - {Label})", ticket.Guid, email.Subject, urgencyScore, sentimentLabel);

        // 3. Estimate Effort
        try
        {
            await _estimatingService.EstimateComplexityAsync(ticket.Guid);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GERDA Estimating failed for ticket {TicketGuid}", ticket.Guid);
        }

        return ticket;
    }
}
