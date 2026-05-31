using System.Reflection;
using RabbitMQ.Client;
using RabbitMqConnector.Interfaces;

namespace RabbitMqConnector;

public class RabbitInitializer : IRabbitInitializer
{
    private readonly IPersistentConnection _persistentConnection;

    public RabbitInitializer(IPersistentConnection persistentConnection)
    {
        _persistentConnection = persistentConnection;
    }

    public async Task InitializeExchangesAsync(Assembly assembly)
    {
        using var channel = await _persistentConnection.CreateChannelAsync();

        var producerTypes = assembly.GetTypes()
            .Where(t => typeof(IProducer).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in producerTypes)
        {
            var attr = type.GetCustomAttribute<RabbitExchangeAttribute>();
            string exchangeName = attr?.Name ?? type.Name;
            string exchangeType = attr?.Type ?? "topic";

            await channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: exchangeType,
                durable: true,
                autoDelete: false);
        }
    }
}
