public interface IEventHandler<T>
{
    Task HandleAsync(T message);
}