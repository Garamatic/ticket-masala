using RabbitMqConnector.Contracts;
using MailingService.Services;

public class TicketAssignedHandler : IEventHandler<TicketAssignedEvent>
{
    private readonly EmailTemplateService _templateService;
    private readonly EmailService _emailService;

    public TicketAssignedHandler(
        EmailTemplateService templateService,
        EmailService emailService)
    {
        _templateService = templateService;
        _emailService = emailService;
    }

    public async Task HandleAsync(TicketAssignedEvent message)
    {
        var html = _templateService.BuildTicketAssignedTemplate(
            message.TicketId,
            message.AssignedTo,
            message.AssignedBy,
            message.AssignedAt
        );

        await _emailService.SendEmailAsync(
            message.CustomerEmail,
            "Your Ticket Has Been Assigned",
            html
        );
    }
}
