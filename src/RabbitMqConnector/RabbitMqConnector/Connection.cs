using RabbitMQ.Client;
using RabbitMqConnector.Interfaces;

namespace RabbitMqConnector;

public class Connection : IPersistentConnection
{
    private readonly IConnectionFactory _connectionFactory;
    private IConnection? _connection;
    private readonly object _lock = new();

    public Connection(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public bool IsConnected => _connection is { IsOpen: true };

    public async Task<IChannel> CreateChannelAsync()
    {
        if (!IsConnected)
        {
            _connection = await _connectionFactory.CreateConnectionAsync();
        }

        return await _connection!.CreateChannelAsync();
    }

    public void Dispose() => _connection?.Dispose();
}
