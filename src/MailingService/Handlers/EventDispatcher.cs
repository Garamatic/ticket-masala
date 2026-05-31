using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RabbitMqConnector.Contracts;

public class EventDispatcher
{
    private readonly IServiceProvider _provider;

    public EventDispatcher(IServiceProvider provider)
    {
        _provider = provider;
    }

    public async Task Dispatch(string eventType, string message)
    {
        switch (eventType)
        {
            case "ticket.created":
                await Handle<TicketCreatedEvent, TicketCreatedHandler>(message);
                break;

            case "ticket.assigned":
                await Handle<TicketAssignedEvent, TicketAssignedHandler>(message);
                break;

            case "ticket.resolved":
                await Handle<TicketResolvedEvent, TicketResolvedHandler>(message);
                break;
            case "invoice.created":
                await Handle<InvoiceCreatedEvent, InvoiceCreatedHandler>(message);
                break;
            case "invoice.overdue":
                await Handle<InvoiceOverdueEvent, InvoiceOverdueHandler>(message);
                break;
            case "payment.received":
                await Handle<PaymentReceivedEvent, PaymentReceivedHandler>(message);
                break;
            case "user.created":
                await Handle<UserCreatedEvent, UserCreatedHandler>(message);
                break;

            default:
                throw new NotSupportedException($"Unknown event type: {eventType}");
        }
    }

    private async Task Handle<T, THandler>(string message)
        where T : RabbitMqConnector.Contracts.IEvent
        where THandler : IEventHandler<T>
    {
        var obj = JsonSerializer.Deserialize<T>(message);

        if (obj == null)
        {
            return;
        }

        var handler = _provider.GetRequiredService<THandler>();
        await handler.HandleAsync(obj);
    }
}
