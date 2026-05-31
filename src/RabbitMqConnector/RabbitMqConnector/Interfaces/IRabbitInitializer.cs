using System.Reflection;

namespace RabbitMqConnector.Interfaces;

public interface IRabbitInitializer
{
    Task InitializeExchangesAsync(Assembly assembly);
}
