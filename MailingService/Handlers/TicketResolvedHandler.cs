using MailingService.Models;
using MailingService.Services;

public class TicketResolvedHandler : IEventHandler<TicketResolvedEvent>
{
    private readonly EmailService _emailService;
    private readonly EmailTemplateService _templateService;

    public TicketResolvedHandler(
        EmailService emailService,
        EmailTemplateService templateService)
    {
        _emailService = emailService;
        _templateService = templateService;
    }

    public async Task HandleAsync(TicketResolvedEvent message)
    {
        var html = _templateService.BuildTicketResolvedTemplate(
            message.CustomerName,
            message.TicketId.ToString(),
            message.ServiceDescription,
            message.Amount,
            message.ResolvedAt,
            message.ResolutionNotes
        );

        await _emailService.SendEmailAsync(
            message.CustomerEmail,
            "Your Ticket Has Been Resolved",
            html
        );
    }
}