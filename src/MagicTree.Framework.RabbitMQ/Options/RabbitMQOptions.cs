namespace MagicTree.Framework.RabbitMQ.Options;

/// <summary>
/// Configuration options for RabbitMQ connection and behavior.
/// Bind from appsettings.json "RabbitMQ" section.
/// </summary>
public class RabbitMQOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public static string SectionName => "RabbitMQ";

    /// <summary>
    /// RabbitMQ host address.
    /// Example: "localhost", "rabbitmq.yourdomain.com"
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ port. Default is 5672 for AMQP, 5671 for AMQPS (SSL).
    /// </summary>
    public int Port { get; set; } = 5672;


    /// <summary>
    /// Username for authentication.
    /// </summary>
    public string Username { get; set; } = "guest";

    /// <summary>
    /// Password for authentication.
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// AutomaticRecoveryEnabled
    /// </summary>
    public bool AutomaticRecoveryEnabled { get; set; } = true;

    /// <summary>
    /// NetworkRecoveryInterval
    /// </summary>
    public int NetworkRecoveryIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Exchange
    /// </summary>
    public string Exchange { get; set; } = "auth-events";

}