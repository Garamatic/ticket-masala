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

    private const string ExchangeName = "event_exchange";
    private const string DlxExchangeName = "event_exchange.dlx";

    private readonly ILogger<Worker> _logger;
    private readonly IConnection _connection;
    private IChannel? _channel;
    private readonly EventDispatcher _dispatcher;

    public Worker(
        ILogger<Worker> logger,
        IConnection connection,
        EventDispatcher eventDispatcher)
    {
        _logger = logger;
        _connection = connection;
        _dispatcher = eventDispatcher;
    }

    public override async Task StartAsync(CancellationToken stoppingToken)
    {
        _channel = await _connection.CreateChannelAsync();

        // Declare main exchange (used by all Garamatic services)
        await _channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

        // Declare dead-letter exchange
        await _channel.ExchangeDeclareAsync(DlxExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: "mailing.queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = DlxExchangeName, ["x-dead-letter-routing-key"] = "mailing.deadletter" },
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: "mailing.deadletter",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync("mailing.deadletter", DlxExchangeName, "mailing.deadletter", cancellationToken: stoppingToken);

        // Bind to all relevant event routing keys (must match handlers in EventDispatcher)
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
            await _channel.QueueBindAsync("mailing.queue", ExchangeName, routingKey, cancellationToken: stoppingToken);
            _logger.LogInformation("Bound mailing.queue to {Exchange}/{RoutingKey}", ExchangeName, routingKey);
        }

        _logger.LogInformation("Worker started, listening on mailing.queue");

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

                _logger.LogInformation("Raw message received: {message}", message);

                var envelope = JsonSerializer.Deserialize<EventEnvelope>(message);

                if (envelope is null)
                {
                    _logger.LogWarning("Could not deserialize envelope");
                    await _channel!.BasicNackAsync(args.DeliveryTag, false, requeue: false);
                    return;
                }

                _logger.LogInformation("Event received: {eventType}", envelope.EventType);

                await _dispatcher.Dispatch(envelope.EventType, message);

                await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false);
            }
            catch (NotSupportedException ex)
            {
                _logger.LogError(ex, "Unknown event type; dead-lettering without retry");
                await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing RabbitMQ message");
                await HandleRetryAsync(args);
            }
        };

        await _channel!.BasicConsumeAsync(
            queue: "mailing.queue",
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleRetryAsync(BasicDeliverEventArgs args)
    {
        var retryCount = 0;
        if (args.BasicProperties.Headers?.TryGetValue("x-retry-count", out var headerValue) == true)
        {
            retryCount = Convert.ToInt32(headerValue);
        }

        if (retryCount >= MaxRetries)
        {
            await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
            _logger.LogError(
                "Max retries ({MaxRetries}) exceeded; dead-lettering delivery tag {DeliveryTag}",
                MaxRetries, args.DeliveryTag);
            return;
        }

        var props = CopyBasicProperties(args.BasicProperties, retryCount + 1);

        try
        {
            await _channel!.BasicPublishAsync(
                exchange: args.Exchange,
                routingKey: args.RoutingKey,
                mandatory: false,
                basicProperties: props,
                body: args.Body,
                cancellationToken: CancellationToken.None);

            await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false);

            _logger.LogWarning(
                "Requeued for retry {RetryCount}/{MaxRetries} (delivery tag {DeliveryTag})",
                retryCount + 1, MaxRetries, args.DeliveryTag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to republish for retry; dead-lettering delivery tag {DeliveryTag}",
                args.DeliveryTag);
            await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private static BasicProperties CopyBasicProperties(IReadOnlyBasicProperties original, int retryCount)
    {
        return new BasicProperties
        {
            Headers = new Dictionary<string, object?> { ["x-retry-count"] = retryCount },
            ContentType = original.ContentType,
            DeliveryMode = original.DeliveryMode,
            CorrelationId = original.CorrelationId,
            MessageId = original.MessageId
        };
    }
}
