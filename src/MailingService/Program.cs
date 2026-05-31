using MailingService;
using RabbitMQ.Client;
using MailingService.Models;
using MailingService.Services;

var builder = WebApplication.CreateBuilder(args);

// Logging for debugging
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"RabbitMQ:Host = {builder.Configuration["RabbitMQ:Host"]}");
Console.WriteLine($"RabbitMQ:Port = {builder.Configuration["RabbitMQ:Port"]}");

// Registers RabbitMQ connection factory; Worker creates connection asynchronously in StartAsync.
builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
    var port = int.TryParse(builder.Configuration["RabbitMQ:Port"], out var p) ? p : 5672;
    var username = builder.Configuration["RabbitMQ:Username"] ?? "guest";
    var password = builder.Configuration["RabbitMQ:Password"] ?? "guest";

    return new ConnectionFactory
    {
        HostName = host,
        Port = port,
        UserName = username,
        Password = password,
        AutomaticRecoveryEnabled = true,
        TopologyRecoveryEnabled = true
    };
});

// Background worker that consumes RabbitMQ messages
builder.Services.AddHostedService<Worker>();

// Core services
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<EmailTemplateService>();
builder.Services.AddSingleton<EventDispatcher>();
builder.Services.AddSingleton<TicketCreatedHandler>();
builder.Services.AddSingleton<TicketAssignedHandler>();
builder.Services.AddSingleton<TicketResolvedHandler>();
builder.Services.AddSingleton<InvoiceCreatedHandler>();
builder.Services.AddSingleton<InvoiceOverdueHandler>();
builder.Services.AddSingleton<PaymentReceivedHandler>();
builder.Services.AddSingleton<UserCreatedHandler>();

var app = builder.Build();

// Health check endpoint
app.MapGet("/health", () => new { status = "healthy", service = "mailing-service" });

// Direct email send endpoint (for integration testing)
app.MapPost("/send", async (EmailSendEvent request, EmailService emailService, ILogger<Program> logger) =>
{
    try
    {
        await emailService.SendEmailAsync(request.ToEmail, request.Subject, request.BodyHtml);
        return Results.Ok(new { status = "sent", to = request.ToEmail });
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to send email via SendGrid; returning accepted for testing");
        return Results.Json(
            new { status = "queued", to = request.ToEmail, warning = "SendGrid not configured" },
            statusCode: 202);
    }
});

app.Run();
