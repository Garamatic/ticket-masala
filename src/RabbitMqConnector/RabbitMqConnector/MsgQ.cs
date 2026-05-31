using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMqConnector.Interfaces;

namespace RabbitMqConnector
{
    public class MsgQ(IPersistentConnection persistentConnection) : IMsgQ   
    {
        private readonly IPersistentConnection _persistentConnection = persistentConnection;

        public async Task SendMessage<T>(T message, RoutingKeys routingKey) where T : IProducer
        {
            if (!_persistentConnection.IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available.");
            }

            var exchangeName = GetExchangeName<T>();

            using var channel = await _persistentConnection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties { Persistent = true };

            await channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: routingKey.ToString(),
                mandatory: false,
                basicProperties: properties,
                body: body);
        }

        private string GetExchangeName<T>()
        {
            var attribute = typeof(T).GetCustomAttribute<RabbitExchangeAttribute>();
            return attribute?.Name ?? typeof(T).Name;
        }
    }
}
