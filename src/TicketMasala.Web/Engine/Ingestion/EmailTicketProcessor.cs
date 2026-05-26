using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.Abstractions;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.GERDA.Estimating;
using TicketMasala.Web.Engine.GERDA.Sentiment;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Engine.Ingestion;

public record EmailContent(string Subject, string Body, string From);

public interface IEmailTicketProcessor
{
    Task<Ticket> ProcessEmailAsync(EmailContent email, CancellationToken cancellationToken);
}

public class EmailTicketProcessor : IEmailTicketProcessor
{
    private readonly ITicketLifecycle _ticketLifecycle;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITicketRepository _ticketRepository;
    private readonly IEstimatingService _estimatingService;
    private readonly ISentimentAnalyzer _sentimentAnalyzer;
    private readonly ISystemClock _clock;
    private readonly ILogger<EmailTicketProcessor> _logger;

    public EmailTicketProcessor(
        ITicketLifecycle ticketLifecycle,
        IUnitOfWork unitOfWork,
        ITicketRepository ticketRepository,
        IEstimatingService estimatingService,
        ISentimentAnalyzer sentimentAnalyzer,
        ISystemClock clock,
        ILogger<EmailTicketProcessor> logger)
    {
        _ticketLifecycle = ticketLifecycle;
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
        _estimatingService = estimatingService;
        _sentimentAnalyzer = sentimentAnalyzer;
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger;
    }

    public async Task<Ticket> ProcessEmailAsync(EmailContent email, CancellationToken cancellationToken)
    {
        // 1. Analyze Sentiment / Urgency
        var (urgencyScore, sentimentLabel) = _sentimentAnalyzer.Analyze(email.Subject, email.Body);

        // 2. Create Ticket via Workflow Service (handles observers, defaults, PII scrubbing)
        // TODO: Look up or create customer by email instead of using system default
        var result = await _ticketLifecycle.ExecuteAsync(
            new CreateTicketCommand(email.Body ?? "(No Content)", "system-email", null, null, _clock.UtcNow.AddDays(7)),
            new TicketContext("system"));

        if (!result.Success)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "Failed to create ticket from email");
        }

        var ticket = result.Ticket!;

        // Enhance with email-specific info (Update ticket returned from creation)
        ticket.Title = email.Subject ?? "(No Subject)";
        ticket.PriorityScore = urgencyScore;
        ticket.GerdaTags = $"Email-Ingested,Sentiment-{sentimentLabel}";

        // Direct save - TODO: migrate to UpdateTicketCommand when it supports full field updates
        // For now, we lose PII scrubbing and observer notifications on email ingestion updates
        await _ticketRepository.UpdateAsync(ticket);
        await _unitOfWork.CommitAsync();

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
