using RabbitMqConnector.Contracts;
using MailingService.Services;

public class TicketCreatedHandler : IEventHandler<TicketCreatedEvent>
{
    private readonly EmailService _emailService;
    private readonly EmailTemplateService _templateService;

    public TicketCreatedHandler(
        EmailService emailService,
        EmailTemplateService templateService)
    {
        _emailService = emailService;
        _templateService = templateService;
    }

    public async Task HandleAsync(TicketCreatedEvent message)
    {
        var html = _templateService.BuildTicketCreatedTemplate(
            message.CustomerName,
            message.TicketId,
            message.Priority,
            message.Description
        );

        await _emailService.SendEmailAsync(
            message.CustomerEmail,
            "Your Ticket Has Been Created",
            html
        );
    }
}
