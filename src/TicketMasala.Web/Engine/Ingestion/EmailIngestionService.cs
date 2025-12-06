using MailKit.Net.Imap;
using MailKit.Search;
using MailKit;
using MimeKit;
using TicketMasala.Web.Models;
using TicketMasala.Web.Services.Tickets;

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
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessEmailsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing emails");
                }

                // Poll every minute
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ProcessEmailsAsync()
        {
            var emailConfig = _configuration.GetSection("EmailSettings");
            var host = emailConfig["Host"];
            var port = int.Parse(emailConfig["Port"] ?? "993");
            var useSsl = bool.Parse(emailConfig["UseSsl"] ?? "true");
            var username = emailConfig["Username"];
            var password = emailConfig["Password"];

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("Email settings not configured. Skipping ingestion.");
                return;
            }

            using var client = new ImapClient();
            await client.ConnectAsync(host, port, useSsl);
            await client.AuthenticateAsync(username, password);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite);

            // Search for unread emails
            var uids = await inbox.SearchAsync(SearchQuery.NotSeen);

            foreach (var uid in uids)
            {
                var message = await inbox.GetMessageAsync(uid);
                
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ITProjectDB>();
                    var ticketService = scope.ServiceProvider.GetRequiredService<ITicketService>();

                    // Check if sender exists as customer
                    var senderEmail = message.From.Mailboxes.FirstOrDefault()?.Address;
                    if (string.IsNullOrEmpty(senderEmail)) continue;

                    var customer = context.Customers.FirstOrDefault(u => u.Email == senderEmail);
                    
                    if (customer == null)
                    {
                        // Create new customer
                        customer = new Customer
                        {
                            UserName = senderEmail,
                            Email = senderEmail,
                            FirstName = message.From.Mailboxes.FirstOrDefault()?.Name ?? "Unknown",
                            LastName = "User",
                            Code = "EMAIL-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                            Phone = "N/A"
                        };
                        context.Customers.Add(customer);
                        await context.SaveChangesAsync();
                    }
                    
                    // Create ticket
                    var ticket = new Ticket
                    {
                        Guid = Guid.NewGuid(),
                        // Ticket has no Title, so we put Subject in Description
                        Description = $"Subject: {message.Subject}\n\n{message.TextBody ?? message.HtmlBody ?? "No content"}",
                        TicketStatus = Status.Pending,
                        TicketType = TicketType.Incident,
                        Customer = customer,
                        CustomerId = customer.Id
                    };

                    context.Tickets.Add(ticket);
                    await context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Created ticket {ticket.Guid} from email {message.Subject}");
                }

                // Mark as seen
                await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true);
            }

            await client.DisconnectAsync(true);
        }
    }
