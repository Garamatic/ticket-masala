using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqConnector.Interfaces
{
    internal interface IMsgQ
    {
        Task SendMessage<T>(T message, RoutingKeys routingKey) where T : IProducer;
    }
}
