using RabbitMqConnector.Contracts;

public interface IEventHandler<T> where T : IEvent
{
    Task HandleAsync(T message);
}
