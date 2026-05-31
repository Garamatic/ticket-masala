using SendGrid;
using SendGrid.Helpers.Mail;

namespace MailingService.Services;

public class EmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly SendGridClient _client;
    private readonly EmailAddress _from;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _client = new SendGridClient(configuration["SendGrid:ApiKey"]!);
        _from = new EmailAddress(
            configuration["SendGrid:FromEmail"]!,
            configuration["SendGrid:FromName"]!
        );
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var to = new EmailAddress(toEmail);

        var msg = MailHelper.CreateSingleEmail(
            _from,
            to,
            subject,
            plainTextContent: "Please view this email in an HTML-compatible client.",
            htmlContent: htmlBody
        );

        var response = await _client.SendEmailAsync(msg);

        _logger.LogInformation(
            "Email sent to {email}, status: {status}",
            toEmail,
            response.StatusCode
        );
    }
}