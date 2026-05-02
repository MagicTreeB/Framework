# MagicTree.Framework.RabbitMQ

Comprehensive RabbitMQ integration package for .NET 10.0 with publisher confirms, automatic retry, dead letter queues, and connection health monitoring.

## Features

✅ **Reliable Message Delivery**
- Publisher confirms for guaranteed delivery
- Automatic retry with exponential backoff
- Dead letter queue for failed messages
- Configurable timeout and retry policies

✅ **Connection Management**
- Automatic connection recovery on network failures
- Connection pooling and channel management
- Topology recovery (exchanges, queues, bindings)
- Health monitoring with background service

✅ **Message Publishing**
- Type-safe message publishing with generics
- Batch publishing for bulk operations
- Custom headers and delayed messages
- Message envelope pattern with metadata

✅ **Message Consumption**
- Async event-driven consumption
- Automatic acknowledgment and retry
- Prefetch control for throughput tuning
- Manual acknowledgment support

✅ **Infrastructure Setup**
- Declarative exchange and queue configuration
- Dead letter queue setup helpers
- Lazy queues for large message volumes
- Queue binding with routing keys

## Installation

### 1. Add Project Reference

Add reference to `MagicTree.Framework.RabbitMQ` in your API project:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\Core\MagicTree.Framework.RabbitMQ\MagicTree.Framework.RabbitMQ.csproj" />
</ItemGroup>
```

### 2. Configure appsettings.json

Add RabbitMQ configuration section:

```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "VirtualHost": "/",
    "Username": "guest",
    "Password": "guest",
    "UseSsl": false,
    "ConnectionTimeoutSeconds": 30,
    "RequestedHeartbeat": 60,
    "AutomaticRecoveryEnabled": true,
    "NetworkRecoveryIntervalSeconds": 5,
    "TopologyRecoveryEnabled": true,
    "ClientProvidedName": "Auth.Api",
    "PrefetchCount": 10,
    "DefaultExchange": "",
    "DefaultExchangeType": "direct",
    "DurableExchanges": true,
    "DurableQueues": true,
    "PublisherConfirms": true,
    "PublisherConfirmTimeoutMs": 5000,
    "RetryPolicy": {
      "Enabled": true,
      "MaxRetryAttempts": 3,
      "InitialRetryDelayMs": 1000,
      "UseExponentialBackoff": true,
      "MaxRetryDelayMs": 60000,
      "DeadLetterExchange": "dead-letter-exchange",
      "DeadLetterQueue": "dead-letter-queue"
    }
  }
}
```

### 3. Register Services in Program.cs

```csharp
using MagicTree.Framework.RabbitMQ.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add RabbitMQ services
builder.Services.AddRabbitMQ(builder.Configuration);

var app = builder.Build();

// Configure infrastructure (exchanges, queues, bindings)
app.Services.ConfigureRabbitMQInfrastructure(connectionManager =>
{
    // Setup dead letter queue
    connectionManager.SetupDefaultDeadLetterQueue();

    // Declare exchanges
    connectionManager.DeclareExchange("user-events", "topic", durable: true);
    connectionManager.DeclareExchange("email-events", "fanout", durable: true);

    // Declare queues with dead letter support
    connectionManager.DeclareQueueWithDeadLetter(
        "user-registration-queue",
        "dead-letter-exchange",
        messageTtlMs: 300000); // 5 minutes TTL

    connectionManager.DeclareQueueWithDeadLetter(
        "email-verification-queue",
        "dead-letter-exchange");

    // Bind queues to exchanges
    connectionManager.BindQueue("user-registration-queue", "user-events", "user.registered");
    connectionManager.BindQueue("email-verification-queue", "email-events", "");
});

app.Run();
```

## Usage Examples

### 1. Publishing a Message

```csharp
using MagicTree.Framework.RabbitMQ.Interfaces;

public class RegisterUserCommandHandler
{
    private readonly IRabbitMQPublisher _publisher;

    public RegisterUserCommandHandler(IRabbitMQPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task<IResult<RegisterUserResponse>> Handle(RegisterUserCommand command)
    {
        // ... register user logic ...

        // Publish user registered event
        var userEvent = new UserRegisteredEvent
        {
            UserId = user.Id,
            Email = user.Email,
            RegisteredAt = DateTimeOffset.UtcNow
        };

        await _publisher.PublishAsync(
            exchange: "user-events",
            routingKey: "user.registered",
            message: userEvent,
            persistent: true);

        return Result.Ok(response);
    }
}
```

### 2. Publishing to Default Exchange

```csharp
// Publish to queue directly (default exchange with routing key = queue name)
await _publisher.PublishAsync(
    routingKey: "email-verification-queue",
    message: emailEvent);
```

### 3. Publishing with Custom Headers

```csharp
var headers = new Dictionary<string, object>
{
    { "priority", "high" },
    { "source", "Auth.Api" },
    { "user-id", userId }
};

await _publisher.PublishWithHeadersAsync(
    exchange: "user-events",
    routingKey: "user.updated",
    message: userEvent,
    headers: headers);
```

### 4. Batch Publishing

```csharp
var events = new List<UserRegisteredEvent>
{
    new() { UserId = userId1, Email = "user1@example.com" },
    new() { UserId = userId2, Email = "user2@example.com" },
    new() { UserId = userId3, Email = "user3@example.com" }
};

await _publisher.PublishBatchAsync(
    exchange: "user-events",
    routingKey: "user.registered",
    messages: events);
```

### 5. Delayed Message Publishing

Requires RabbitMQ delayed message plugin:

```csharp
// Publish with 5 second delay
await _publisher.PublishDelayedAsync(
    exchange: "delayed-exchange", // Must be x-delayed-message type
    routingKey: "password-reset",
    message: resetEvent,
    delayMs: 5000);
```

### 6. Consuming Messages

```csharp
using MagicTree.Framework.RabbitMQ.Interfaces;

public class EmailVerificationConsumerService : IHostedService
{
    private readonly IRabbitMQConsumer _consumer;
    private readonly ILogger<EmailVerificationConsumerService> _logger;

    public EmailVerificationConsumerService(
        IRabbitMQConsumer consumer,
        ILogger<EmailVerificationConsumerService> logger)
    {
        _consumer = consumer;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _consumer.ConsumeAsync<EmailVerificationEvent>(
            queueName: "email-verification-queue",
            handler: async (message, context) =>
            {
                try
                {
                    _logger.LogInformation(
                        "Processing email verification for user {UserId}. MessageId: {MessageId}",
                        message.UserId, context.MessageId);

                    // Send verification email
                    await SendVerificationEmailAsync(message);

                    // Return true to ACK (acknowledge) message
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process email verification for user {UserId}", message.UserId);
                    
                    // Return false to NACK (negative acknowledge) and requeue
                    return false;
                }
            },
            cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _consumer.StopAllAsync();
    }
}
```

### 7. Consuming with Automatic Retry

```csharp
// Consumer with automatic retry and dead letter queue
await _consumer.ConsumeWithRetryAsync<UserRegisteredEvent>(
    queueName: "user-registration-queue",
    handler: async (message, context) =>
    {
        _logger.LogInformation(
            "Processing user registration. UserId: {UserId}, RetryCount: {RetryCount}",
            message.UserId, context.RetryCount);

        // Process message
        await ProcessUserRegistrationAsync(message);

        return true; // Success
    },
    cancellationToken);
```

### 8. Topic Exchange Routing

```csharp
// Publisher
await _publisher.PublishAsync("logs", "error.api.auth", errorLog);
await _publisher.PublishAsync("logs", "info.api.auth", infoLog);
await _publisher.PublishAsync("logs", "warning.api.mmo", warningLog);

// Consumer 1: All error logs
connectionManager.DeclareQueue("error-logs-queue");
connectionManager.BindQueue("error-logs-queue", "logs", "error.*");

// Consumer 2: All API logs
connectionManager.DeclareQueue("api-logs-queue");
connectionManager.BindQueue("api-logs-queue", "logs", "*.api.*");

// Consumer 3: All logs
connectionManager.DeclareQueue("all-logs-queue");
connectionManager.BindQueue("all-logs-queue", "logs", "#");
```

### 9. Fanout Exchange (Broadcasting)

```csharp
// Setup
connectionManager.DeclareExchange("notifications", "fanout", durable: true);
connectionManager.DeclareQueue("email-notifications-queue");
connectionManager.DeclareQueue("sms-notifications-queue");
connectionManager.DeclareQueue("push-notifications-queue");
connectionManager.BindQueue("email-notifications-queue", "notifications", "");
connectionManager.BindQueue("sms-notifications-queue", "notifications", "");
connectionManager.BindQueue("push-notifications-queue", "notifications", "");

// Publish to all consumers
await _publisher.PublishAsync("notifications", "", notificationEvent);
```

### 10. RPC Pattern (Request/Reply)

```csharp
// Publisher (Request)
var correlationId = Guid.NewGuid().ToString();
var replyQueue = "reply-queue-" + correlationId;

connectionManager.DeclareQueue(replyQueue, exclusive: true, autoDelete: true);

var headers = new Dictionary<string, object>
{
    { "correlation-id", correlationId },
    { "reply-to", replyQueue }
};

await _publisher.PublishWithHeadersAsync("rpc-exchange", "process", request, headers);

// Consumer (Reply)
await _consumer.ConsumeAsync<ProcessRequest>(
    queueName: "process-queue",
    handler: async (request, context) =>
    {
        var response = await ProcessAsync(request);

        // Send reply
        if (!string.IsNullOrEmpty(context.ReplyTo))
        {
            var replyHeaders = new Dictionary<string, object>
            {
                { "correlation-id", context.CorrelationId ?? "" }
            };

            await _publisher.PublishWithHeadersAsync(
                "", // Default exchange
                context.ReplyTo,
                response,
                replyHeaders);
        }

        return true;
    },
    cancellationToken);
```

## Configuration Options

### Connection Settings

| Property | Default | Description |
|----------|---------|-------------|
| `Host` | `"localhost"` | RabbitMQ server hostname |
| `Port` | `5672` | AMQP port (5672 for plain, 5671 for SSL) |
| `VirtualHost` | `"/"` | Virtual host for isolation |
| `Username` | `"guest"` | Authentication username |
| `Password` | `"guest"` | Authentication password |
| `UseSsl` | `false` | Enable SSL/TLS encryption |

### Connection Behavior

| Property | Default | Description |
|----------|---------|-------------|
| `ConnectionTimeoutSeconds` | `30` | Connection timeout |
| `RequestedHeartbeat` | `60` | Heartbeat interval (0 = disabled) |
| `AutomaticRecoveryEnabled` | `true` | Auto-reconnect on failure |
| `NetworkRecoveryIntervalSeconds` | `5` | Delay between reconnection attempts |
| `TopologyRecoveryEnabled` | `true` | Recreate exchanges/queues on recovery |

### Performance Tuning

| Property | Default | Description |
|----------|---------|-------------|
| `PrefetchCount` | `10` | Unacknowledged messages per consumer (1 = strict round-robin, higher = better throughput) |
| `PublisherConfirms` | `true` | Wait for broker confirmation (reliable delivery) |
| `PublisherConfirmTimeoutMs` | `5000` | Publisher confirm timeout |

### Retry Policy

| Property | Default | Description |
|----------|---------|-------------|
| `Enabled` | `true` | Enable automatic retry |
| `MaxRetryAttempts` | `3` | Maximum retry count before dead letter |
| `InitialRetryDelayMs` | `1000` | Initial retry delay (1 second) |
| `UseExponentialBackoff` | `true` | Exponential backoff: delay = initial * 2^attempt |
| `MaxRetryDelayMs` | `60000` | Maximum retry delay cap (60 seconds) |
| `DeadLetterExchange` | `"dead-letter-exchange"` | Dead letter exchange name |
| `DeadLetterQueue` | `"dead-letter-queue"` | Dead letter queue name |

## Exchange Types

### Direct Exchange
Routes messages by exact routing key match.

```csharp
connectionManager.DeclareExchange("direct-exchange", "direct");
connectionManager.BindQueue("queue1", "direct-exchange", "routing.key1");
connectionManager.BindQueue("queue2", "direct-exchange", "routing.key2");
```

### Fanout Exchange
Broadcasts messages to all bound queues (ignores routing key).

```csharp
connectionManager.DeclareExchange("fanout-exchange", "fanout");
connectionManager.BindQueue("queue1", "fanout-exchange", "");
connectionManager.BindQueue("queue2", "fanout-exchange", "");
```

### Topic Exchange
Routes messages by pattern matching on routing key.

Patterns:
- `*` - matches exactly one word
- `#` - matches zero or more words

```csharp
connectionManager.DeclareExchange("topic-exchange", "topic");
connectionManager.BindQueue("error-logs", "topic-exchange", "error.*");
connectionManager.BindQueue("all-logs", "topic-exchange", "#");
connectionManager.BindQueue("api-logs", "topic-exchange", "*.api.*");
```

### Headers Exchange
Routes by matching message headers (not routing key).

```csharp
connectionManager.DeclareExchange("headers-exchange", "headers");

var arguments = new Dictionary<string, object>
{
    { "x-match", "all" }, // "all" or "any"
    { "format", "pdf" },
    { "type", "report" }
};

connectionManager.BindQueue("pdf-reports", "headers-exchange", "", arguments);
```

## Best Practices

### 1. Durable Queues and Persistent Messages
Always use durable queues and persistent messages for critical data:

```csharp
connectionManager.DeclareQueue("orders-queue", durable: true);
await _publisher.PublishAsync("orders", "new", order, persistent: true);
```

### 2. Publisher Confirms
Enable publisher confirms for reliable delivery:

```json
"PublisherConfirms": true,
"PublisherConfirmTimeoutMs": 5000
```

### 3. Prefetch Count Tuning
- Set `PrefetchCount = 1` for fair distribution (strict round-robin)
- Set `PrefetchCount = 10+` for better throughput (consumer batching)
- Tune based on message processing time

### 4. Error Handling
Always handle errors gracefully and return appropriate acknowledgment:

```csharp
handler: async (message, context) =>
{
    try
    {
        await ProcessAsync(message);
        return true; // ACK
    }
    catch (TransientException ex)
    {
        _logger.LogWarning(ex, "Transient error, will retry");
        return false; // NACK with requeue
    }
    catch (PermanentException ex)
    {
        _logger.LogError(ex, "Permanent error, sending to dead letter");
        return false; // NACK without requeue (goes to dead letter)
    }
}
```

### 5. Idempotency
Messages may be delivered multiple times. Make handlers idempotent:

```csharp
handler: async (message, context) =>
{
    // Check if already processed
    if (await _repository.IsAlreadyProcessedAsync(context.MessageId))
    {
        _logger.LogWarning("Message already processed: {MessageId}", context.MessageId);
        return true; // ACK to avoid reprocessing
    }

    // Process message
    await ProcessAsync(message);

    // Mark as processed
    await _repository.MarkAsProcessedAsync(context.MessageId);

    return true;
}
```

### 6. Dead Letter Queue Monitoring
Monitor dead letter queue for failed messages:

```csharp
await _consumer.ConsumeAsync<MessageEnvelope<object>>(
    queueName: "dead-letter-queue",
    handler: async (message, context) =>
    {
        var failureReason = context.Headers?["x-failure-reason"] as string;
        var retryCount = context.RetryCount;

        _logger.LogError(
            "Dead letter message: {MessageId}, Reason: {Reason}, RetryCount: {RetryCount}",
            context.MessageId, failureReason, retryCount);

        // Alert, log to monitoring system, or manual intervention
        await AlertOpsTeamAsync(message, failureReason, retryCount);

        return true; // Remove from dead letter queue
    },
    cancellationToken);
```

### 7. Logging and Monitoring
Use structured logging with correlation IDs:

```csharp
_logger.LogInformation(
    "Processing message. MessageId: {MessageId}, CorrelationId: {CorrelationId}, RetryCount: {RetryCount}",
    context.MessageId, context.CorrelationId, context.RetryCount);
```

## Troubleshooting

### Connection Failures
- Check RabbitMQ server is running: `docker ps` or `rabbitmqctl status`
- Verify credentials: Username/Password in appsettings.json
- Check network connectivity and firewall rules
- Review connection logs in application and RabbitMQ server

### Message Loss
- Enable `PublisherConfirms = true` for guaranteed delivery
- Use `persistent = true` when publishing
- Declare queues with `durable = true`
- Monitor publisher confirm timeouts in logs

### Performance Issues
- Increase `PrefetchCount` for better consumer throughput
- Use batch publishing for bulk operations
- Enable lazy queues for large message volumes
- Monitor RabbitMQ memory and disk usage

### Consumer Not Receiving Messages
- Verify queue exists and has messages: RabbitMQ Management UI
- Check consumer is running: Look for "Started consuming" log
- Verify queue binding: Check exchange → routing key → queue
- Check prefetch count and acknowledgment mode

### Dead Letter Queue Filling Up
- Review failure reasons in dead letter message headers
- Fix application bugs causing message rejection
- Increase `MaxRetryAttempts` if transient errors
- Implement manual message recovery process

## Dependencies

- **RabbitMQ.Client** - AMQP 0-9-1 protocol client
- **Microsoft.Extensions.Configuration.Abstractions** - Configuration support
- **Microsoft.Extensions.DependencyInjection.Abstractions** - DI registration
- **Microsoft.Extensions.Hosting.Abstractions** - Background service support
- **Microsoft.Extensions.Logging.Abstractions** - Logging support
- **Microsoft.Extensions.Options** - Options pattern
- **System.Text.Json** - Message serialization

## Related Documentation

- [RabbitMQ Official Documentation](https://www.rabbitmq.com/documentation.html)
- [AMQP 0-9-1 Protocol](https://www.rabbitmq.com/amqp-0-9-1-reference.html)
- [Publisher Confirms](https://www.rabbitmq.com/confirms.html)
- [Consumer Acknowledgements](https://www.rabbitmq.com/confirms.html#consumer-acknowledgements)
- [Dead Letter Exchanges](https://www.rabbitmq.com/dlx.html)
