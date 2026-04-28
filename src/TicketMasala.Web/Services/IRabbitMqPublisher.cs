namespace TicketMasala.Web.Services;

/// <summary>
/// Interface for publishing messages to RabbitMQ.
/// Provides async methods for connecting, publishing, and closing the connection.
/// </summary>
public interface IRabbitMqPublisher
{
    /// <summary>
    /// Establishes connection to RabbitMQ and initializes the publisher.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a message to the specified routing key.
    /// </summary>
    /// <typeparam name="T">Type of the message payload</typeparam>
    /// <param name="message">The message to publish</param>
    /// <param name="routingKey">The routing key for the message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the connection to RabbitMQ.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CloseAsync(CancellationToken cancellationToken = default);
}
