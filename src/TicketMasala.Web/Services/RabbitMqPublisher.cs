using System.Text.Json;
using System.Text.Json.Serialization;
using RabbitMQ.Client;

namespace TicketMasala.Web.Services;

/// <summary>
/// Publishes events to RabbitMQ with snake_case JSON serialization.
/// Uses publisher confirms for reliable delivery.
/// </summary>
public class RabbitMqPublisher : IRabbitMqPublisher, IAsyncDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _isConnected;

    public RabbitMqPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqPublisher> logger)
    {
        _options = configuration.GetSection("RabbitMQ").Get<RabbitMqOptions>() ?? new RabbitMqOptions();
        _logger = logger;

        // Configure snake_case JSON serialization per IC-001 convention
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_isConnected)
        {
            return;
        }

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);

            // Enable publisher confirms for reliable delivery
            _channel = await _connection.CreateChannelAsync(
                new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true),
                cancellationToken);

            // Declare the exchange
            await _channel.ExchangeDeclareAsync(
                _options.ExchangeName,
                ExchangeType.Topic,
                durable: true,
                cancellationToken: cancellationToken);

            _isConnected = true;

            _logger.LogInformation(
                "Connected to RabbitMQ at {Host}:{Port}, exchange: {Exchange}",
                _options.HostName,
                _options.Port,
                _options.ExchangeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ at {Host}:{Port}",
                _options.HostName, _options.Port);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default)
    {
        if (!_isConnected || _channel == null)
        {
            await ConnectAsync(cancellationToken);
        }

        if (_channel == null)
        {
            throw new InvalidOperationException("RabbitMQ channel is not initialized");
        }

        try
        {
            var body = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);

            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await _channel.BasicPublishAsync(
                _options.ExchangeName,
                routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogDebug(
                "Published {EventType} to {RoutingKey} on {Exchange}",
                typeof(T).Name,
                routingKey,
                _options.ExchangeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish {EventType} to {RoutingKey}",
                typeof(T).Name,
                routingKey);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_channel != null)
            {
                await _channel.CloseAsync(cancellationToken);
                _channel.Dispose();
                _channel = null;
            }

            if (_connection != null)
            {
                await _connection.CloseAsync(cancellationToken);
                _connection.Dispose();
                _connection = null;
            }

            _isConnected = false;

            _logger.LogInformation("Disconnected from RabbitMQ");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while closing RabbitMQ connection");
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
    }
}
