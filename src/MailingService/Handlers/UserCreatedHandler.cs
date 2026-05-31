using RabbitMqConnector.Contracts;
using MailingService.Services;

public class UserCreatedHandler : IEventHandler<UserCreatedEvent>
{
    private readonly EmailService _emailService;
    private readonly EmailTemplateService _templateService;

    public UserCreatedHandler(
        EmailService emailService,
        EmailTemplateService templateService)
    {
        _emailService = emailService;
        _templateService = templateService;
    }

    public async Task HandleAsync(UserCreatedEvent message)
    {
        var html = _templateService.BuildUserCreatedTemplate(
            message.Name,
            message.UserId,
            message.Email,
            message.Role,
            message.CreatedAt
        );

        await _emailService.SendEmailAsync(
            message.Email,
            "Welcome to Garamatic!",
            html
        );
    }
}
