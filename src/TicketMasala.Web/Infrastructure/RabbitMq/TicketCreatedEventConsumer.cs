using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TicketMasala.Domain.Entities;
using TicketMasala.Domain.Repositories;
using TicketMasala.Web.Repositories;

namespace TicketMasala.Web.Infrastructure.RabbitMq;

/// <summary>
/// RabbitMQ consumer that listens for <c>event.ticket.created</c> messages and
/// creates tickets in the database. Uses a dedicated retry queue with TTL so
/// transient failures do not cause duplicate delivery to other consumers.
/// </summary>
public class TicketCreatedEventConsumer : BackgroundService
{
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 30000;

    private const string ExchangeName = "event_exchange";
    private const string RetryExchangeName = "ticketmasala.ticket.created.retry.exchange";

    private const string QueueName = "ticketmasala.ticket.created";
    private const string RetryQueue = "ticketmasala.ticket.created.retry";

    private const string RoutingKey = "event.ticket.created";
    private const string RetryRoutingKey = "ticket.created.retry";
    private const string RetryReturnRoutingKey = "ticket.created.retry.return";

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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Connect and declare topology in the background (not in StartAsync) so a
        // RabbitMQ outage at boot neither blocks host startup nor crashes the app.
        // If the broker never comes up, the consumer stops quietly and the rest of
        // the web app keeps running.
        if (!await TryConnectAndDeclareTopologyAsync(stoppingToken))
        {
            return;
        }

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
                var source = GetString(eventData, "source");
                var customerEmail = GetString(eventData, "customer_email");
                var customerName = GetString(eventData, "customer_name");
                var description = GetString(eventData, "description");
                var priority = GetString(eventData, "priority");
                var tags = GetString(eventData, "tags");

                // Prevent circular dependency: outbox events from this system
                // must not trigger another ticket creation.
                if (source == "ticket-masala")
                {
                    _logger.LogInformation(
                        "Skipping internal outbox event (source=ticket-masala) for ticket {TicketId}",
                        ticketId);
                    await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false);
                    return;
                }

                _logger.LogInformation(
                    "Processing ticket.created event: TicketId={TicketId}, Source={Source}, Email={Email}",
                    ticketId, source, customerEmail);

                using var scope = _serviceProvider.CreateScope();
                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var ticketRepository = scope.ServiceProvider.GetRequiredService<ITicketRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                // Idempotency: if this ticket was already created, skip it
                var ticketGuid = Guid.Empty;
                var ticketExists = false;
                if (!string.IsNullOrEmpty(ticketId) && Guid.TryParse(ticketId, out ticketGuid))
                {
                    ticketExists = await ticketRepository.GetByIdAsync(ticketGuid) is not null;
                }
                if (ticketExists)
                {
                    _logger.LogInformation(
                        "Ticket {TicketGuid} already exists; acknowledging duplicate event",
                        ticketGuid);
                    await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false);
                    return;
                }

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

                // Create ticket, preserving the external ticket_id for idempotency
                var ticket = Ticket.CreateFromPortal(
                    description,
                    customer?.Id,
                    priorityScore: priorityScore,
                    tags: tags,
                    completionTarget: DateTime.UtcNow.AddDays(7),
                    guid: ticketGuid != Guid.Empty ? ticketGuid : null);

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
                        "Error processing ticket.created event; routing to retry ({RetryCount}/{MaxRetries})",
                        retryCount + 1, MaxRetries);

                    var props = new BasicProperties
                    {
                        Headers = new Dictionary<string, object?> { ["x-retry-count"] = retryCount + 1 },
                        ContentType = args.BasicProperties.ContentType,
                        DeliveryMode = args.BasicProperties.DeliveryMode,
                        CorrelationId = args.BasicProperties.CorrelationId,
                        MessageId = args.BasicProperties.MessageId
                    };

                    try
                    {
                        await _channel!.BasicPublishAsync(
                            exchange: RetryExchangeName,
                            routingKey: RetryRoutingKey,
                            mandatory: false,
                            basicProperties: props,
                            body: args.Body,
                            cancellationToken: CancellationToken.None);

                        await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false);
                    }
                    catch (Exception publishEx)
                    {
                        _logger.LogError(publishEx,
                            "Failed to route to retry for {MessageId}; dead-lettering",
                            args.BasicProperties.MessageId);
                        await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
                    }
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

    /// <summary>
    /// Connects to RabbitMQ (with retry/backoff) and declares the exchanges,
    /// queues, and bindings. Returns <c>false</c> if the broker is unreachable
    /// after all attempts or shutdown is requested, so the caller can stop the
    /// consumer without throwing — keeping the host alive.
    /// </summary>
    private async Task<bool> TryConnectAndDeclareTopologyAsync(CancellationToken stoppingToken)
    {
        const int retries = 10;
        const int retryDelayMs = 2000;
        IChannel? channel = null;
        for (int i = 0; i < retries; i++)
        {
            if (stoppingToken.IsCancellationRequested) return false;
            try
            {
                _connection = await _connectionFactory.CreateConnectionAsync(stoppingToken);
                channel = await _connection.CreateChannelAsync();
                _channel = channel;
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Failed to connect to RabbitMQ (attempt {Attempt}/{MaxRetries})", i + 1, retries);
                if (i == retries - 1)
                {
                    _logger.LogCritical("Could not connect to RabbitMQ after {MaxRetries} attempts. Consumer will not start.", retries);
                    return false;
                }
                await Task.Delay(retryDelayMs, stoppingToken);
            }
        }

        if (channel == null) return false;

        // Fair dispatch: one message at a time
        await channel.BasicQosAsync(0, 1, false, cancellationToken: stoppingToken);

        // Main exchange (topic)
        await channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

        // Retry exchange (messages routed here go to the retry queue)
        await channel.ExchangeDeclareAsync(RetryExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

        // Main queue: failed messages go to retry exchange first
        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = RetryExchangeName,
                ["x-dead-letter-routing-key"] = RetryRoutingKey
            },
            cancellationToken: stoppingToken);

        // Retry queue: messages sit here for RetryDelayMs, then route back to main queue
        await channel.QueueDeclareAsync(
            queue: RetryQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-message-ttl"] = RetryDelayMs,
                ["x-dead-letter-exchange"] = ExchangeName,
                ["x-dead-letter-routing-key"] = RetryReturnRoutingKey
            },
            cancellationToken: stoppingToken);

        // Bindings
        await channel.QueueBindAsync(QueueName, ExchangeName, RoutingKey, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(QueueName, ExchangeName, RetryReturnRoutingKey, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(RetryQueue, RetryExchangeName, RetryRoutingKey, cancellationToken: stoppingToken);

        _logger.LogInformation(
            "TicketCreatedEventConsumer started on {Queue}. Retry: {RetryQueue} ({RetryDelayMs}ms TTL)",
            QueueName, RetryQueue, RetryDelayMs);

        return true;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TicketCreatedEventConsumer stopping...");

        // Cancel the stopping token and wait for ExecuteAsync to complete
        await base.StopAsync(cancellationToken);

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
