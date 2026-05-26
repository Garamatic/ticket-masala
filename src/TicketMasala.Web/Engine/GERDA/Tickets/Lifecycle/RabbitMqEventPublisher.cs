using TicketMasala.Web.Messaging;

namespace TicketMasala.Web.Engine.GERDA.Tickets.Lifecycle;

/// <summary>
/// Adapter: bridges the existing IRabbitMqPublisher to the new IEventPublisher port.
/// </summary>
internal sealed class RabbitMqEventPublisher : IEventPublisher
{
    private readonly IRabbitMqPublisher? _publisher;

    public RabbitMqEventPublisher(IRabbitMqPublisher? publisher)
    {
        _publisher = publisher;
    }

    public async Task PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default)
    {
        if (_publisher == null)
            return;

        await _publisher.PublishAsync(message, routingKey, cancellationToken);
    }
}
