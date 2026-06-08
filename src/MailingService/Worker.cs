using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using MailingService.Models;
using MailingService.Services;

namespace MailingService;

public class Worker : BackgroundService
{
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 30000;

    private const string ExchangeName = "event_exchange";
    private const string DlxExchangeName = "event_exchange.dlx";
    private const string RetryExchangeName = "mailing.retry.exchange";

    private const string MainQueue = "mailing.queue";
    private const string RetryQueue = "mailing.retry.queue";
    private const string DeadLetterQueue = "mailing.deadletter";

    private readonly ILogger<Worker> _logger;
    private readonly IConnectionFactory _connectionFactory;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly EventDispatcher _dispatcher;

    public Worker(
        ILogger<Worker> logger,
        IConnectionFactory connectionFactory,
        EventDispatcher eventDispatcher)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
        _dispatcher = eventDispatcher;
    }

    public override async Task StartAsync(CancellationToken stoppingToken)
    {
        _connection = await _connectionFactory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync();

        // Declare main exchange
        await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

        // Declare dead-letter exchange
        await _channel.ExchangeDeclareAsync(DlxExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

        // Declare retry exchange (messages routed here go to retry queue with TTL)
        await _channel.ExchangeDeclareAsync(RetryExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

        // Main queue: failed messages go to retry exchange first
        await _channel.QueueDeclareAsync(
            queue: MainQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = RetryExchangeName,
                ["x-dead-letter-routing-key"] = "mailing.retry"
            },
            cancellationToken: stoppingToken);

        // Retry queue: messages sit here for RetryDelayMs, then route back to main exchange
        await _channel.QueueDeclareAsync(
            queue: RetryQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-message-ttl"] = RetryDelayMs,
                ["x-dead-letter-exchange"] = ExchangeName,
                ["x-dead-letter-routing-key"] = "mailing.retry.return"
            },
            cancellationToken: stoppingToken);

        // Dead-letter queue: for messages that exceeded max retries
        await _channel.QueueDeclareAsync(
            queue: DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        // Bindings
        await _channel.QueueBindAsync(RetryQueue, RetryExchangeName, "mailing.retry", cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(DeadLetterQueue, DlxExchangeName, "mailing.deadletter", cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(MainQueue, ExchangeName, "mailing.retry.return", cancellationToken: stoppingToken);

        // Bind to all relevant event routing keys
        var routingKeys = new[]
        {
            "event.ticket.created",
            "event.ticket.assigned",
            "event.ticket.resolved",
            "event.invoice.created",
            "event.invoice.overdue",
            "event.payment.received"
        };

        foreach (var routingKey in routingKeys)
        {
            await _channel.QueueBindAsync(MainQueue, ExchangeName, routingKey, cancellationToken: stoppingToken);
            _logger.LogInformation("Bound {Queue} to {Exchange}/{RoutingKey}", MainQueue, ExchangeName, routingKey);
        }

        _logger.LogInformation(
            "Worker started, listening on {Queue}. Retry: {RetryQueue} ({RetryDelayMs}ms TTL), DeadLetter: {DeadLetterQueue}",
            MainQueue, RetryQueue, RetryDelayMs, DeadLetterQueue);

        await base.StartAsync(stoppingToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel!);

        consumer.ReceivedAsync += async (_, args) =>
        {
            using var activity = new System.Diagnostics.Activity("MailingService.ProcessMessage");
            if (args.BasicProperties.CorrelationId is not null)
            {
                activity.SetParentId(args.BasicProperties.CorrelationId);
            }
            activity.Start();

            try
            {
                var message = Encoding.UTF8.GetString(args.Body.Span);

                _logger.LogInformation(
                    "Message received: {MessageId} (Correlation: {CorrelationId}) - {EventType}",
                    args.BasicProperties.MessageId,
                    args.BasicProperties.CorrelationId,
                    message);

                var envelope = JsonSerializer.Deserialize<EventEnvelope>(message);

                if (envelope is null)
                {
                    _logger.LogWarning(
                        "Could not deserialize envelope for {MessageId}; dead-lettering",
                        args.BasicProperties.MessageId);
                    await _channel!.BasicNackAsync(args.DeliveryTag, false, requeue: false);
                    return;
                }

                _logger.LogInformation(
                    "Event received: {EventType} (MessageId: {MessageId})",
                    envelope.EventType,
                    args.BasicProperties.MessageId);

                await _dispatcher.Dispatch(envelope.EventType, message);

                await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false);
            }
            catch (NotSupportedException ex)
            {
                _logger.LogError(
                    ex,
                    "Unknown event type for {MessageId}; dead-lettering without retry",
                    args.BasicProperties.MessageId);
                await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing message {MessageId}; routing to retry or dead-letter",
                    args.BasicProperties.MessageId);
                await HandleFailureAsync(args, ex);
            }
            finally
            {
                activity.Stop();
            }
        };

        await _channel!.BasicConsumeAsync(
            queue: MainQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleFailureAsync(BasicDeliverEventArgs args, Exception ex)
    {
        var retryCount = 0;
        if (args.BasicProperties.Headers?.TryGetValue("x-retry-count", out var headerValue) == true)
        {
            retryCount = Convert.ToInt32(headerValue);
        }

        if (retryCount >= MaxRetries)
        {
            // Max retries exceeded: dead-letter to permanent failure queue
            await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
            _logger.LogError(
                "Max retries ({MaxRetries}) exceeded for {MessageId}; dead-lettering delivery tag {DeliveryTag}",
                MaxRetries,
                args.BasicProperties.MessageId,
                args.DeliveryTag);
            return;
        }

        // Republish to retry exchange with incremented retry count
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
                routingKey: "mailing.retry",
                mandatory: false,
                basicProperties: props,
                body: args.Body,
                cancellationToken: CancellationToken.None);

            await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false);

            _logger.LogWarning(
                "Scheduled retry {RetryCount}/{MaxRetries} for {MessageId} (delivery tag {DeliveryTag})",
                retryCount + 1,
                MaxRetries,
                args.BasicProperties.MessageId,
                args.DeliveryTag);
        }
        catch (Exception publishEx)
        {
            _logger.LogError(publishEx,
                "Failed to schedule retry for {MessageId}; dead-lettering delivery tag {DeliveryTag}",
                args.BasicProperties.MessageId,
                args.DeliveryTag);
            await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker stopping...");

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
}
