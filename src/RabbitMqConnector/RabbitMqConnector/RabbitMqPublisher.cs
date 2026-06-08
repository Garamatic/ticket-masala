using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMqConnector.Interfaces;

namespace RabbitMqConnector;

/// <summary>
/// Shared RabbitMQ publisher using RabbitMQ.Client v7.x async API.
/// Supports publisher confirms, lazy initialization, and OpenTelemetry tracing.
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
    private const string ConfigSection = "RabbitMQ";
    private const string ExchangeNameKey = "ExchangeName";
    public const string DefaultExchangeName = "event_exchange";

    private static readonly ActivitySource ActivitySource = new ActivitySource("RabbitMqConnector.Messaging");

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
    private readonly bool _publisherConfirms;
    private readonly ILogger<RabbitMqPublisher>? _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _exchangeDeclared;
    private bool _disposed;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public RabbitMqPublisher(
        string hostName,
        int port,
        string userName,
        string password,
        string virtualHost,
        string exchangeName,
        bool publisherConfirms = false,
        ILogger<RabbitMqPublisher>? logger = null)
    {
        _hostName = hostName;
        _port = port;
        _userName = userName;
        _password = password;
        _virtualHost = virtualHost;
        _exchangeName = exchangeName;
        _publisherConfirms = publisherConfirms;
        _logger = logger;
    }

    public RabbitMqPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqPublisher>? logger = null)
    {
        _logger = logger;
        _exchangeName = ResolveExchangeName(configuration);

        var section = configuration.GetSection(ConfigSection);
        _hostName = section["HostName"] ?? "localhost";
        _port = section.GetValue<int?>("Port") ?? 5672;
        _userName = section["UserName"] ?? "guest";
        _password = section["Password"] ?? "guest";
        _virtualHost = section["VirtualHost"] ?? "/";
        _publisherConfirms = section.GetValue<bool?>("PublisherConfirms") ?? false;
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

        using var activity = ActivitySource.StartActivity("RabbitMq.Publish");
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", routingKey);
        activity?.SetTag("messaging.message_type", typeof(T).Name);

        var channel = await EnsureInitializedAsync(cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            MessageId = Guid.NewGuid().ToString("N"),
            CorrelationId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N")
        };

        await channel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger?.LogDebug(
            "Published {EventType} to {Exchange}/{RoutingKey} (MessageId: {MessageId})",
            typeof(T).Name,
            _exchangeName,
            routingKey,
            properties.MessageId);
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
                        publisherConfirmationsEnabled: _publisherConfirms,
                        publisherConfirmationTrackingEnabled: _publisherConfirms),
                    cancellationToken);

                _logger?.LogInformation(
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
                _logger?.LogInformation("RabbitMQ exchange declared: {ExchangeName}", _exchangeName);
            }

            return _channel;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex,
                "Failed to initialize RabbitMQ publisher (host: {Host}:{Port})",
                _hostName,
                _port);

            // Clean up partially created resources and reset flags
            await CleanupAsync();
            _exchangeDeclared = false;

            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task CleanupAsync()
    {
        if (_channel is not null)
        {
            try { await _channel.CloseAsync(); } catch { /* ignore */ }
            try { await _channel.DisposeAsync(); } catch { /* ignore */ }
            _channel = null;
        }
        if (_connection is not null)
        {
            try { await _connection.CloseAsync(); } catch { /* ignore */ }
            try { await _connection.DisposeAsync(); } catch { /* ignore */ }
            _connection = null;
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
            _logger?.LogWarning("Timeout waiting for RabbitMQ publisher lock during disposal");
        }

        if (lockAcquired)
        {
            try
            {
                await CleanupAsync();
            }
            finally
            {
                _initLock.Release();
            }
        }

        _initLock.Dispose();
    }
}
