using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqConnector.Interfaces
{
    public interface IConsumer { }
    public interface IConsumer<T> : IConsumer
    where T : IProducer
    {
        Task HandleMessage(T message);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class RabbitQueueAttribute : Attribute
    {
        public string QueueName { get; }
        public string RoutingKey { get; }

        public RabbitQueueAttribute(string queueName, RoutingKeys routingKey)
        {
            QueueName = queueName;
            RoutingKey = routingKey.ToString();
        }
    }
}
