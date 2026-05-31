using RabbitMqConnector.Contracts;
using MailingService.Services;

public class PaymentReceivedHandler : IEventHandler<PaymentReceivedEvent>
{
    private readonly EmailService _emailService;
    private readonly EmailTemplateService _templateService;

    public PaymentReceivedHandler(
        EmailService emailService,
        EmailTemplateService templateService)
    {
        _emailService = emailService;
        _templateService = templateService;
    }

    public async Task HandleAsync(PaymentReceivedEvent message)
    {
        var html = _templateService.BuildPaymentReceivedTemplate(
            message.InvoiceId,
            message.OdooInvoiceId,
            message.Amount,
            message.PaymentMethod,
            message.PaidAt
        );

        await _emailService.SendEmailAsync(
            message.CustomerEmail,
            "Payment Received - Thank You!",
            html
        );
    }
}
