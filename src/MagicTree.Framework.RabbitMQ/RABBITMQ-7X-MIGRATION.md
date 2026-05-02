# RabbitMQ.Client 7.x Migration Issue

## Problem
MagicTree.Framework.RabbitMQ package was built for RabbitMQ.Client 6.x but the project uses RabbitMQ.Client 7.0+, which has breaking API changes.

## Breaking Changes in RabbitMQ.Client 7.x

### 1. **IModel → IChannel**
- Old: `IModel channel = connection.CreateModel();`
- New: `IChannel channel = await connection.CreateChannelAsync();`

### 2. **Event Handlers**
- Old: `consumer.Received += (sender, args) => { }`
- New: `consumer.ReceivedAsync += async (sender, args) => { }`

### 3. **Synchronous Methods → Async**
- Old: `channel.BasicAck(deliveryTag, false);`
- New: `await channel.BasicAckAsync(deliveryTag, false);`

### 4. **Connection Factory**
- Removed: `DispatchConsumersAsync` property
- Consumers are async by default in 7.x

### 5. **Connection Events**
- Old: `connection.ConnectionShutdown`, `connection.CallbackException`
- New: Different event model with async handlers

### 6. **IAutorecoveringConnection**
- Interface changed or removed in 7.x

## Temporary Workaround

For this OTP forgot password implementation, we have two options:

### **Option A: Simple In-Process Event Bus (Recommended for MVP)**
Instead of RabbitMQ, use MediatR notifications for now:
- Auth.Api publishes ForgotPasswordEvent via MediatR
- Email.Api subscribes to event via MediatR handler
- No external message broker required

**Benefits:**
- ✅ Works immediately without RabbitMQ setup
- ✅ Simpler local development
- ✅ Same event contracts (ForgotPasswordEvent, etc.)

**Drawbacks:**
- ❌ Not distributed (single process)
- ❌ No message persistence/retry

### **Option B: Upgrade MagicTree.Framework.RabbitMQ to 7.x**
Full migration to async RabbitMQ.Client 7.x API:
- Update all IModel → IChannel
- Change all methods to async
- Update event handlers to ReceivedAsync
- Rewrite connection management

**Benefits:**
- ✅ Fully distributed messaging
- ✅ Message persistence and retry
- ✅ Production-ready

**Drawbacks:**
- ❌ Significant refactoring effort (50+ changes)
- ❌ Requires RabbitMQ server running

## Recommendation
1. **For now:** Comment out RabbitMQ, use direct email service calls from handlers
2. **Later:** Complete RabbitMQ 7.x migration as separate task

## Implementation Plan
1. Remove RabbitMQ publisher calls from ForgotPasswordRequest handler
2. Inject IUnosendEmailService directly
3. Send emails synchronously (or queue to background job)
4. Later: Migrate to RabbitMQ 7.x or use Kafka/Azure Service Bus

## Status
- **Current:** Build failing due to RabbitMQ 7.x incompatibility (22 errors)
- **Next:** Implement temporary workaround to unblock forgot password feature
- **Future:** Full RabbitMQ 7.x migration (tracked separately)
