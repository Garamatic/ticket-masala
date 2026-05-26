namespace TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;

/// <summary>
/// Port for publishing integration events.
/// Production: RabbitMQ publisher. Tests: no-op capture.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default);
}
