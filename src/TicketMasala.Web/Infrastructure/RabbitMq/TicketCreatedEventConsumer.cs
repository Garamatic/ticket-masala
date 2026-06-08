using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TicketMasala.Domain.Common;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Repositories;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Infrastructure.RabbitMq;

/// <summary>
/// RabbitMQ consumer that listens for <c>event.ticket.created</c> messages and
/// creates tickets in the database. This closes the ingestion loop: the gatekeeper
/// publishes <c>ticket.created</c> events, and this consumer materialises them.
/// </summary>
public class TicketCreatedEventConsumer : BackgroundService
{
    private const int MaxRetries = 3;
    private const string ExchangeName = "event_exchange";
    private const string QueueName = "ticketmasala.ticket.created";
    private const string RoutingKey = "event.ticket.created";
    private const string DlxExchangeName = "event_exchange.dlx";

    private readonly IConnectionFactory _connectionFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TicketCreatedEventConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public TicketCreatedEventConsumer(
        IConnectionFactory connectionFactory,
        IServiceProvider serviceProvider,
        ILogger<TicketCreatedEventConsumer> logger)
    {
        _connectionFactory = connectionFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken stoppingToken)
    {
        _connection = await _connectionFactory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync();

        // Declare exchange
        await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

        // Declare dead-letter exchange
        await _channel.ExchangeDeclareAsync(DlxExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

        // Declare main queue with DLX
        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = DlxExchangeName,
                ["x-dead-letter-routing-key"] = $"{QueueName}.dlq"
            },
            cancellationToken: stoppingToken);

        // Bind to ticket.created routing key
        await _channel.QueueBindAsync(QueueName, ExchangeName, RoutingKey, cancellationToken: stoppingToken);

        _logger.LogInformation(
            "TicketCreatedEventConsumer started on queue {Queue} bound to {Exchange}/{RoutingKey}",
            QueueName, ExchangeName, RoutingKey);

        await base.StartAsync(stoppingToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel!);

        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                var message = Encoding.UTF8.GetString(args.Body.Span);
                var body = JsonSerializer.Deserialize<JsonElement>(message);

                // Support both wrapped { data: {...}, eventType: "..." } and flat formats
                var eventData = body.TryGetProperty("data", out var dataProp)
                    ? dataProp
                    : body;

                var ticketId = GetString(eventData, "ticket_id");
                var customerEmail = GetString(eventData, "customer_email");
                var customerName = GetString(eventData, "customer_name");
                var description = GetString(eventData, "description");
                var priority = GetString(eventData, "priority");
                var tags = GetString(eventData, "tags");

                _logger.LogInformation(
                    "Processing ticket.created event: TicketId={TicketId}, Email={Email}, Name={Name}",
                    ticketId, customerEmail, customerName);

                using var scope = _serviceProvider.CreateScope();
                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var ticketRepository = scope.ServiceProvider.GetRequiredService<ITicketRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                // Find or create customer
                ApplicationUser? customer = null;
                if (!string.IsNullOrEmpty(customerEmail))
                {
                    customer = await userRepository.GetUserByEmailAsync(customerEmail);
                    if (customer == null)
                    {
                        var names = customerName.Split(' ', 2);
                        customer = new ApplicationUser
                        {
                            UserName = customerEmail,
                            Email = customerEmail,
                            FirstName = names[0],
                            LastName = names.Length > 1 ? names[1] : "User",
                            EmailConfirmed = true
                        };

                        var created = await userRepository.CreateCustomerAsync(customer, "Portal@123");
                        if (!created)
                        {
                            _logger.LogError("Failed to create customer {Email} from RabbitMQ event", customerEmail);
                            await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
                            return;
                        }

                        _logger.LogInformation("Created customer {Email} from RabbitMQ event", customerEmail);
                    }
                }

                // Parse priority
                var priorityScore = priority?.ToLowerInvariant() switch
                {
                    "high" or "10" => 10.0,
                    "medium" or "5" => 5.0,
                    "low" or "1" => 1.0,
                    _ => 5.0
                };

                // Create ticket
                var ticket = Ticket.CreateFromPortal(
                    description,
                    customer?.Id,
                    priorityScore: priorityScore,
                    tags: tags,
                    completionTarget: DateTime.UtcNow.AddDays(7));

                await ticketRepository.AddAsync(ticket);
                await unitOfWork.CommitAsync();

                _logger.LogInformation(
                    "Created ticket {TicketGuid} from RabbitMQ event for customer {CustomerId}",
                    ticket.Guid, customer?.Id ?? "(unknown)");

                await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON in ticket.created event; dead-lettering");
                await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
            }
            catch (Exception ex)
            {
                var retryCount = GetRetryCount(args.BasicProperties.Headers);
                if (retryCount < MaxRetries)
                {
                    _logger.LogWarning(ex,
                        "Error processing ticket.created event; retrying ({RetryCount}/{MaxRetries})",
                        retryCount + 1, MaxRetries);

                    // Republish with incremented retry count
                    var props = new BasicProperties
                    {
                        Headers = new Dictionary<string, object?> { ["x-retry-count"] = retryCount + 1 },
                        ContentType = args.BasicProperties.ContentType,
                        DeliveryMode = args.BasicProperties.DeliveryMode,
                        CorrelationId = args.BasicProperties.CorrelationId,
                        MessageId = args.BasicProperties.MessageId
                    };

                    await _channel!.BasicPublishAsync(
                        exchange: ExchangeName,
                        routingKey: RoutingKey,
                        mandatory: false,
                        basicProperties: props,
                        body: args.Body,
                        cancellationToken: CancellationToken.None);

                    await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false);
                }
                else
                {
                    _logger.LogError(ex,
                        "Max retries exceeded for ticket.created event; dead-lettering");
                    await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
                }
            }
        };

        await _channel!.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TicketCreatedEventConsumer stopping...");

        if (_channel is not null)
        {
            try { await _channel.CloseAsync(); } catch { /* ignore */ }
            try { await _channel.DisposeAsync(); } catch { /* ignore */ }
        }

        if (_connection is not null)
        {
            try { await _connection.CloseAsync(); } catch { /* ignore */ }
            try { await _connection.DisposeAsync(); } catch { /* ignore */ }
        }

        await base.StopAsync(cancellationToken);
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString() ?? string.Empty;

        return string.Empty;
    }

    private static int GetRetryCount(IDictionary<string, object?>? headers)
    {
        if (headers?.TryGetValue("x-retry-count", out var value) == true && value != null)
        {
            return value switch
            {
                int i => i,
                long l => (int)l,
                byte[] b => int.TryParse(Encoding.UTF8.GetString(b), out var parsed) ? parsed : 0,
                string s => int.TryParse(s, out var parsed) ? parsed : 0,
                _ => 0
            };
        }
        return 0;
    }
}
