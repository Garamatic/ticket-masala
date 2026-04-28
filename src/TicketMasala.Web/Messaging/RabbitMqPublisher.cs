using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RabbitMQ.Client;

namespace TicketMasala.Web.Messaging;

/// <summary>
/// Publishes domain events to RabbitMQ for downstream services.
/// Uses RabbitMQ.Client v7.x async API with publisher confirms.
/// All messages are serialized as snake_case JSON to match integration-contracts convention.
/// </summary>
public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(T message, string routingKey) where T : class;
}

/// <summary>
/// Singleton publisher. One channel is shared across all callers. Exchange is declared lazily.
/// </summary>
public class RabbitMqPublisher : IRabbitMqPublisher, IAsyncDisposable
{
    private const string ConfigSection = "RabbitMq";
    private const string ExchangeNameKey = "ExchangeName";
    public const string DefaultExchangeName = "event_exchange";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IConnection _connection;
    private readonly string _exchangeName;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private IChannel? _channel;
    private bool _exchangeDeclared;
    private bool _disposed;

    public RabbitMqPublisher(
        IConnection connection,
        IConfiguration configuration,
        ILogger<RabbitMqPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
        _exchangeName = ResolveExchangeName(configuration);
    }

    public static string ResolveExchangeName(IConfiguration configuration)
    {
        var section = configuration.GetSection(ConfigSection);
        if (section.Exists())
        {
            var configured = section[ExchangeNameKey];
            if (!string.IsNullOrWhiteSpace(configured))
                return configured;
        }
        return DefaultExchangeName;
    }

    public async Task PublishAsync<T>(T message, string routingKey) where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var channel = _channel ?? await GetOrCreateChannelAsync();

        if (!_exchangeDeclared)
        {
            await channel.ExchangeDeclareAsync(
                _exchangeName,
                ExchangeType.Topic,
                durable: true);
            _exchangeDeclared = true;
            _logger.LogInformation("RabbitMQ exchange declared: {ExchangeName}", _exchangeName);
        }

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await channel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body);

        _logger.LogInformation(
            "Published {EventType} to {Exchange}/{RoutingKey}",
            typeof(T).Name,
            _exchangeName,
            routingKey);
    }

    private async Task<IChannel> GetOrCreateChannelAsync()
    {
        if (_channel is not null)
            return _channel;

        _channel = await _connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true));
        _logger.LogInformation("RabbitMQ publisher channel created with confirms enabled");
        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_channel is not null)
        {
            await _channel.CloseAsync();
            await _channel.DisposeAsync();
            _channel = null;
        }
    }
}
