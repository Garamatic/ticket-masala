using TicketMasala.Web.AI;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Common;
using TicketMasala.Web.Engine.Core;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using TicketMasala.Web.Engine.GERDA.Tickets;
using TicketMasala.Web.Data;

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
                using var scope = _serviceProvider.CreateScope();
                // In a real implementation, we would resolve generic services here to create tickets
                // e.g., var ticketService = scope.ServiceProvider.GetRequiredService<ITicketService>();

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
            await inbox.OpenAsync(FolderAccess.ReadWrite, stoppingToken);

            _logger.LogInformation("Total messages: {Count}", inbox.Count);

            // Search for unread messages
            var uids = await inbox.SearchAsync(SearchQuery.NotSeen, stoppingToken);
            _logger.LogInformation("Found {Count} unread messages", uids.Count);

            foreach (var uid in uids)
            {
                var message = await inbox.GetMessageAsync(uid, stoppingToken);
                _logger.LogInformation("Processing email: {Subject} from {From}", message.Subject, message.From);

                // TODO: Convert email to Ticket entity here
                
                // Mark as seen
                await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true, stoppingToken);
            }

            await client.DisconnectAsync(true, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process emails via IMAP");
        }
    }
}
