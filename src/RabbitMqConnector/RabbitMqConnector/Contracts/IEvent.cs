namespace RabbitMqConnector.Contracts;

/// <summary>
/// Marker interface for all integration events exchanged via RabbitMQ.
/// </summary>
public interface IEvent
{
    string EventType { get; }
}
