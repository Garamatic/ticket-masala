using RabbitMqConnector.Contracts;
using MailingService.Services;

public class InvoiceOverdueHandler : IEventHandler<InvoiceOverdueEvent>
{
    private readonly EmailService _emailService;
    private readonly EmailTemplateService _templateService;

    public InvoiceOverdueHandler(
        EmailService emailService,
        EmailTemplateService templateService)
    {
        _emailService = emailService;
        _templateService = templateService;
    }

    public async Task HandleAsync(InvoiceOverdueEvent message)
    {
        var html = _templateService.BuildInvoiceOverdueTemplate(
            message.InvoiceId,
            message.OdooInvoiceId.ToString(),
            message.Amount,
            message.DaysOverdue
        );

        await _emailService.SendEmailAsync(
            message.CustomerEmail,
            "Payment Reminder: Invoice Overdue",
            html
        );
    }
}
