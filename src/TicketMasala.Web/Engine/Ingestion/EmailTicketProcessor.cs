using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.GERDA.Tickets;
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
    private readonly ITicketWorkflowService _ticketWorkflowService;
    private readonly IEstimatingService _estimatingService;
    private readonly ILogger<EmailTicketProcessor> _logger;

    public EmailTicketProcessor(
        ITicketWorkflowService ticketWorkflowService,
        IEstimatingService estimatingService,
        ILogger<EmailTicketProcessor> logger)
    {
        _ticketWorkflowService = ticketWorkflowService;
        _estimatingService = estimatingService;
        _logger = logger;
    }

    public async Task<Ticket> ProcessEmailAsync(EmailContent email, CancellationToken cancellationToken)
    {
        // 1. Analyze Sentiment / Urgency
        var (urgencyScore, sentimentLabel) = SimpleSentimentAnalyzer.Analyze(email.Subject, email.Body);

        // 2. Create Ticket via Workflow Service (handles observers, defaults, PII scrubbing)
        var ticket = await _ticketWorkflowService.CreateTicketAsync(
            description: email.Body ?? "(No Content)",
            customerId: "system-email", // Or look up user
            responsibleId: null,
            projectGuid: null,
            completionTarget: DateTime.UtcNow.AddDays(7)
        );

        // Enhance with email-specific info (Update ticket returned from creation)
        ticket.Title = email.Subject ?? "(No Subject)";
        ticket.PriorityScore = urgencyScore;
        ticket.GerdaTags = $"Email-Ingested,Sentiment-{sentimentLabel}";

        await _ticketWorkflowService.UpdateTicketAsync(ticket);

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
