
using RabbitMqConnector.Interfaces;

namespace RabbitMqConnector
{
    public abstract class BaseHandler<T>  where T : IProducer
    {
        public abstract Task HandleMessage(T message);
    }
}
