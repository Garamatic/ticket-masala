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
    Task PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default);
}

/// <summary>
/// Singleton publisher with lazy connection initialization.
/// Connection, channel, and exchange are created on first publish to avoid blocking startup.
/// Thread-safe for concurrent publish operations.
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

    private readonly string _hostName;
    private readonly int _port;
    private readonly string _userName;
    private readonly string _password;
    private readonly string _virtualHost;
    private readonly string _exchangeName;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _exchangeDeclared;
    private bool _disposed;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public RabbitMqPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;
        _exchangeName = ResolveExchangeName(configuration);

        var section = configuration.GetSection(ConfigSection);
        _hostName = section["HostName"] ?? "localhost";
        _port = section.GetValue<int?>("Port") ?? 5672;
        _userName = section["UserName"] ?? "guest";
        _password = section["Password"] ?? "guest";
        _virtualHost = section["VirtualHost"] ?? "/";
    }

    private IConnectionFactory CreateConnectionFactory()
    {
        return new ConnectionFactory
        {
            HostName = _hostName,
            Port = _port,
            UserName = _userName,
            Password = _password,
            VirtualHost = _virtualHost,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };
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

    public async Task PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);

        var channel = await EnsureInitializedAsync(cancellationToken);

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
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogDebug(
            "Published {EventType} to {Exchange}/{RoutingKey}",
            typeof(T).Name,
            _exchangeName,
            routingKey);
    }

    /// <summary>
    /// Ensures connection, channel, and exchange are initialized.
    /// Thread-safe using double-checked locking pattern.
    /// </summary>
    private async Task<IChannel> EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        // Fast path: already fully initialized
        if (_channel is not null && _exchangeDeclared)
            return _channel;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_channel is not null && _exchangeDeclared)
                return _channel;

            // Create connection if needed
            if (_connection is null)
            {
                _connection = await CreateConnectionFactory().CreateConnectionAsync(cancellationToken);
            }

            // Create channel if needed
            if (_channel is null)
            {
                _channel = await _connection.CreateChannelAsync(
                    new CreateChannelOptions(
                        publisherConfirmationsEnabled: true,
                        publisherConfirmationTrackingEnabled: true),
                    cancellationToken);

                _logger.LogInformation(
                    "RabbitMQ channel created (host: {Host}:{Port}, exchange: {Exchange})",
                    _hostName,
                    _port,
                    _exchangeName);
            }

            // Declare exchange if needed (idempotent operation, safe to repeat)
            if (!_exchangeDeclared)
            {
                await _channel.ExchangeDeclareAsync(
                    _exchangeName,
                    ExchangeType.Topic,
                    durable: true,
                    cancellationToken: cancellationToken);
                _exchangeDeclared = true;
                _logger.LogInformation("RabbitMQ exchange declared: {ExchangeName}", _exchangeName);
            }

            return _channel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to initialize RabbitMQ publisher (host: {Host}:{Port})",
                _hostName,
                _port);

            // Clean up partially created resources and reset flags
            if (_channel is not null)
            {
                try
                { await _channel.CloseAsync(); }
                catch { /* ignore */ }
                try
                { await _channel.DisposeAsync(); }
                catch { /* ignore */ }
                _channel = null;
            }
            if (_connection is not null)
            {
                try
                { await _connection.CloseAsync(); }
                catch { /* ignore */ }
                try
                { await _connection.DisposeAsync(); }
                catch { /* ignore */ }
                _connection = null;
            }
            _exchangeDeclared = false;

            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        // Attempt to acquire lock with timeout, but always dispose the lock
        var lockAcquired = false;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await _initLock.WaitAsync(cts.Token);
            lockAcquired = true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Timeout waiting for RabbitMQ publisher lock during disposal");
        }

        if (lockAcquired)
        {
            try
            {
                if (_channel is not null)
                {
                    try
                    { await _channel.CloseAsync(); }
                    catch { /* ignore */ }
                    await _channel.DisposeAsync();
                    _channel = null;
                }

                if (_connection is not null)
                {
                    try
                    { await _connection.CloseAsync(); }
                    catch { /* ignore */ }
                    await _connection.DisposeAsync();
                    _connection = null;
                }
            }
            finally
            {
                _initLock.Release();
            }
        }

        _initLock.Dispose();
    }
}
