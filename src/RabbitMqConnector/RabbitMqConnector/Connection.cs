using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMqConnector;
using RabbitMqConnector.Interfaces;
using static System.Net.Mime.MediaTypeNames;

namespace RabbitMqConnector
{
    public class Connection : IPersistentConnection
    {
        private readonly IConnectionFactory _connectionFactory;
        private IConnection? _connection;
        private readonly object _lock = new();

        public Connection(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public bool IsConnected => _connection is { IsOpen: true };

        public async Task<IChannel> CreateChannelAsync()
        {
            if (!IsConnected)
            {
                _connection = await _connectionFactory.CreateConnectionAsync();
            }

            return await _connection!.CreateChannelAsync();
        }

        public void Dispose() => _connection?.Dispose();
    }
}
public interface IRabbitInitializer
{
    Task InitializeExchangesAsync(Assembly assembly);
}

public class RabbitInitializer : IRabbitInitializer
{
    private readonly IPersistentConnection _persistentConnection;

    public RabbitInitializer(IPersistentConnection persistentConnection)
    {
        _persistentConnection = persistentConnection;
    }

    public async Task InitializeExchangesAsync(Assembly assembly)
    {
        // Use the wrapper to get the channel
        using var channel = await _persistentConnection.CreateChannelAsync();

        var producerTypes = assembly.GetTypes()
            .Where(t => typeof(IProducer).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in producerTypes)
        {
            var attr = type.GetCustomAttribute<RabbitExchangeAttribute>();
            string exchangeName = attr?.Name ?? type.Name;
            string exchangeType = attr?.Type ?? "topic";

            await channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: exchangeType,
                durable: true,
                autoDelete: false);
        }
    }
}
public static class RabbitMqConnectorExtensions
{
    public static IServiceCollection AddRabbitMqConnector(this IServiceCollection services, string connectionstring)
    {

        services.AddSingleton<IConnectionFactory>(sp =>
        {
            var factory = new ConnectionFactory();
            if (Uri.TryCreate(connectionstring, UriKind.Absolute, out var uri))
            {
                factory.Uri = uri;
            }
            else
            {
                factory.HostName = connectionstring; // Fallback for plain IP/Host
            }
            factory.UserName = "admin";
            factory.Password = "admin123";
            return factory;
        });

        services.AddSingleton<IPersistentConnection, Connection>();
        services.AddSingleton<IRabbitInitializer, RabbitInitializer>();
        services.AddSingleton<IMsgQ, MsgQ>();
        services.AddSingleton<MsgQ>(sp => (MsgQ)sp.GetRequiredService<IMsgQ>());

        return services;
    }

    public static async Task UseRabbitMqAutoDiscovery(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IRabbitInitializer>();

        var assembly = Assembly.GetEntryAssembly();
        if (assembly != null)
        {
            await initializer.InitializeExchangesAsync(assembly);
        }
    }
    public static IServiceCollection AddRabbitMqConsumers(this IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>)));

        foreach (var handlerType in handlerTypes)
        {
            var interfaceType = handlerType.GetInterfaces().First(i => i.GetGenericTypeDefinition() == typeof(IConsumer<>));
            var dtoType = interfaceType.GetGenericArguments()[0];

            services.AddScoped(handlerType);
            var listenerType = typeof(RabbitBackgroundListener<,>).MakeGenericType(dtoType, handlerType);

            services.AddSingleton(typeof(IHostedService), sp =>
                ActivatorUtilities.CreateInstance(sp, listenerType));
        }

        return services;
    }
}

//var builder = WebApplication.CreateBuilder(args);

//// Step 1: Register
//builder.Services.AddRabbitMqConnector("localhost");

//var app = builder.Build();

//// Step 2: Initialize (This runs once at startup)
//await app.Services.UseRabbitMqAutoDiscovery();
//await app.Services.Us 
//app.Run();

