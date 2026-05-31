using MailingService.Models;
using MailingService.Services;

public class InvoiceCreatedHandler : IEventHandler<InvoiceCreatedEvent>
{
    private readonly EmailService _emailService;
    private readonly EmailTemplateService _templateService;

    public InvoiceCreatedHandler(
        EmailService emailService,
        EmailTemplateService templateService)
    {
        _emailService = emailService;
        _templateService = templateService;
    }

    public async Task HandleAsync(InvoiceCreatedEvent message)
    {
        var html = _templateService.BuildInvoiceCreatedTemplate(
            message.InvoiceId?.ToString() ?? "",
            message.OdooInvoiceId,
            message.TicketId.ToString(),
            message.Amount,
            message.Currency,
            message.Status,
            message.CreatedAt
        );

        await _emailService.SendEmailAsync(
            message.CustomerEmail,
            "Your Invoice Is Ready",
            html
        );
    }
}