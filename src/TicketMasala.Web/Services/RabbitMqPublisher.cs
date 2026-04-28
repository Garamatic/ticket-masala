using System.Text.Json;
using System.Text.Json.Serialization;
using RabbitMQ.Client;

namespace TicketMasala.Web.Services;

/// <summary>
/// Publishes events to RabbitMQ with snake_case JSON serialization.
/// Uses publisher confirms for reliable delivery.
/// This class is thread-safe and should be registered as a singleton.
/// </summary>
public class RabbitMqPublisher : IRabbitMqPublisher, IAsyncDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _isConnected;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private const int LockTimeoutMs = 30000; // 30 second timeout for lock acquisition

    public RabbitMqPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqPublisher> logger)
    {
        _options = configuration.GetSection("RabbitMQ").Get<RabbitMqOptions>() ?? new RabbitMqOptions();
        _logger = logger;

        // Validate options early
        ValidateOptions(_options);

        // Configure snake_case JSON serialization per IC-001 convention
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
    }

    private static void ValidateOptions(RabbitMqOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.HostName))
            throw new ArgumentException("RabbitMQ HostName is required.", nameof(options.HostName));

        if (options.Port <= 0 || options.Port > 65535)
            throw new ArgumentException("RabbitMQ Port must be between 1 and 65535.", nameof(options.Port));

        if (string.IsNullOrWhiteSpace(options.ExchangeName))
            throw new ArgumentException("RabbitMQ ExchangeName is required.", nameof(options.ExchangeName));
    }

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_isConnected && _channel is not null && !_channel.IsClosed)
        {
            return;
        }

        // Use timeout to prevent indefinite waits
        if (!await _connectionLock.WaitAsync(LockTimeoutMs, cancellationToken))
        {
            throw new TimeoutException($"Failed to acquire connection lock within {LockTimeoutMs}ms");
        }

        try
        {
            // Double-check after acquiring lock
            if (_isConnected && _channel is not null && !_channel.IsClosed)
            {
                return;
            }

            await ConnectInternalAsync(cancellationToken);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task ConnectInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Clean up any existing broken connection
            await CleanupConnectionAsync();

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

    private async Task CleanupConnectionAsync()
    {
        try
        {
            if (_channel is not null)
            {
                await _channel.CloseAsync();
                _channel.Dispose();
                _channel = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error cleaning up RabbitMQ channel during reconnect");
        }

        try
        {
            if (_connection is not null)
            {
                await _connection.CloseAsync();
                _connection.Dispose();
                _connection = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error cleaning up RabbitMQ connection during reconnect");
        }

        _isConnected = false;
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);

        await ConnectAsync(cancellationToken);

        if (!await _connectionLock.WaitAsync(LockTimeoutMs, cancellationToken))
        {
            throw new TimeoutException($"Failed to acquire publish lock within {LockTimeoutMs}ms");
        }

        try
        {
            if (_channel is null || _channel.IsClosed)
            {
                throw new InvalidOperationException("RabbitMQ channel is not available");
            }

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
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            return;
        }

        if (!await _connectionLock.WaitAsync(LockTimeoutMs, cancellationToken))
        {
            _logger.LogWarning("Failed to acquire lock for closing connection within {Timeout}ms", LockTimeoutMs);
            return;
        }

        try
        {
            await CleanupConnectionAsync();
            _logger.LogInformation("Disconnected from RabbitMQ");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while closing RabbitMQ connection");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await CloseAsync();
        }
        catch (Exception ex)
        {
            // Ensure we log any unexpected errors during close, but still dispose the lock
            _logger.LogError(ex, "Unexpected error during RabbitMQ publisher disposal");
        }
        finally
        {
            _connectionLock.Dispose();
        }
    }
}
