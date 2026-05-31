using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client.Events;
using RabbitMqConnector.Interfaces;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;


namespace RabbitMqConnector
{
 
    // We inherit BackgroundService, which handles the IHostedService plumbing for us
    public class RabbitBackgroundListener<T, THandler> : BackgroundService
        where T : IProducer
        where THandler : IConsumer<T>
    {
        private readonly IPersistentConnection _connection;
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitQueueAttribute _queueSettings;
        private readonly string _exchangeName;

        public RabbitBackgroundListener(IPersistentConnection connection, IServiceProvider serviceProvider)
        {
            _connection = connection;
            _serviceProvider = serviceProvider;

            _queueSettings = typeof(THandler).GetCustomAttribute<RabbitQueueAttribute>()
                ?? throw new Exception($"Handler {typeof(THandler).Name} must have a [RabbitQueue] attribute.");

            _exchangeName = typeof(T).GetCustomAttribute<RabbitExchangeAttribute>()?.Name ?? typeof(T).Name;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var channel = await _connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(_queueSettings.QueueName, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(_queueSettings.QueueName, _exchangeName, _queueSettings.RoutingKey);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<THandler>();

                var body = ea.Body.ToArray();
                var message = JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(body));

                if (message != null)
                {
                    await handler.HandleMessage(message);
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
            };

            await channel.BasicConsumeAsync(
                queue: _queueSettings.QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
