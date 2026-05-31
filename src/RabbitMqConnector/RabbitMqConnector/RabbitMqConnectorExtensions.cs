using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMqConnector.Interfaces;

namespace RabbitMqConnector;

public static class RabbitMqConnectorExtensions
{
    /// <summary>
    /// Registers the shared RabbitMqPublisher as a singleton using configuration from IConfiguration.
    /// </summary>
    public static IServiceCollection AddRabbitMqPublisher(this IServiceCollection services)
    {
        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
        return services;
    }

    /// <summary>
    /// Registers the legacy RabbitMqConnector infrastructure (connection, MsgQ, consumers).
    /// Consider using AddRabbitMqPublisher instead for new code.
    /// </summary>
    public static IServiceCollection AddRabbitMqConnector(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IConnectionFactory>(sp =>
        {
            var factory = new ConnectionFactory();
            if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            {
                factory.Uri = uri;
            }
            else
            {
                factory.HostName = connectionString;
            }
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
