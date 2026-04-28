namespace TicketMasala.Web.Services;

/// <summary>
/// Configuration options for RabbitMQ connection.
/// </summary>
public class RabbitMqOptions
{
    /// <summary>
    /// RabbitMQ server hostname or IP address.
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ server port (default: 5672).
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// RabbitMQ username.
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// RabbitMQ password.
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Exchange name for publishing events.
    /// </summary>
    public string ExchangeName { get; set; } = "garamatic.events";

    /// <summary>
    /// Virtual host (default: "/").
    /// </summary>
    public string VirtualHost { get; set; } = "/";
}
