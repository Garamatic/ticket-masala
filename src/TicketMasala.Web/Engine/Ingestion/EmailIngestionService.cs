using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Web.AI;
using TicketMasala.Web.Data;
using TicketMasala.Web.Engine.Core;
using TicketMasala.Web.Engine.GERDA.Tickets;

namespace TicketMasala.Web.Engine.Ingestion;

public class EmailIngestionService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailIngestionService> _logger;
    private readonly IConfiguration _configuration;

    public EmailIngestionService(IServiceProvider serviceProvider, ILogger<EmailIngestionService> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email Ingestion Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in email ingestion service");
            }

            // Poll every 5 minutes
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }

        _logger.LogInformation("Email Ingestion Service stopped");
    }

    private async Task ProcessEmailsAsync(CancellationToken stoppingToken)
    {
        var settings = _configuration.GetSection("EmailSettings").Get<Configuration.EmailSettings>();

        if (settings == null || string.IsNullOrEmpty(settings.Host))
        {
            _logger.LogWarning("Email settings not configured, skipping ingestion.");
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.Username) || string.IsNullOrWhiteSpace(settings.Password))
        {
            _logger.LogWarning("Email settings missing credentials, skipping ingestion.");
            return;
        }

        _logger.LogInformation("Connecting to IMAP server {Host}:{Port}...", settings.Host, settings.Port);

        try
        {
            using var client = new ImapClient();

            // Connect
            await client.ConnectAsync(settings.Host, settings.Port, settings.UseSsl, stoppingToken);

            // Authenticate
            await client.AuthenticateAsync(settings.Username, settings.Password, stoppingToken);

            _logger.LogInformation("Authenticated as {User}", settings.Username);

            // Open Inbox
            var inbox = client.Inbox;
            if (inbox == null)
            {
                _logger.LogWarning("IMAP inbox folder unavailable, skipping ingestion cycle.");
                return;
            }
            await inbox.OpenAsync(FolderAccess.ReadWrite, stoppingToken);

            // Search for unread messages
            var uids = await inbox.SearchAsync(SearchQuery.NotSeen, stoppingToken);
            _logger.LogInformation("Found {Count} unread messages", uids.Count);

            if (uids.Count == 0)
                return;

            // Create scope for DB Context and Processor
            using var scope = _serviceProvider.CreateScope();
            // Note: We need to ensure IEmailTicketProcessor is registered in DI
            var processor = scope.ServiceProvider.GetRequiredService<IEmailTicketProcessor>();

            foreach (var uid in uids)
            {
                var message = await inbox.GetMessageAsync(uid, stoppingToken);
                _logger.LogInformation("Processing email: {Subject} from {From}", message.Subject, message.From);

                var emailContent = new EmailContent(
                    message.Subject ?? "(no subject)",
                    message.TextBody ?? message.HtmlBody ?? "",
                    message.From.ToString());

                try
                {
                    await processor.ProcessEmailAsync(emailContent, stoppingToken);

                    // Mark as seen only if successful
                    await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process email {Subject}", message.Subject);
                }
            }

            await client.DisconnectAsync(true, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process emails via IMAP");
        }
    }
}
