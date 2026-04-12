[中文](README.cn.md)

# Senparc NCF EventBus - High-Concurrency Event Bus

## Overview

A high-performance in-memory event bus built on `System.Threading.Channels.Channel`, supporting:
- High-concurrency event processing (configurable concurrency level)
- Duplicate event detection (prevents the same event from being processed multiple times)
- Automatic failure retry (exponential backoff strategy)
- Cross-module decoupled communication (avoids circular dependencies)
- Async non-blocking publishing (publisher does not wait for processing to complete)

## Core Components

### 1. IEventBus - Event Bus Interface
```csharp
public interface IEventBus
{
    ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
```

### 2. IntegrationEvent - Event Base Class
```csharp
public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid Id { get; } = Guid.NewGuid();  // Auto-generated unique ID
    public DateTime CreationDate { get; } = DateTime.UtcNow;
}
```

### 3. IIntegrationEventHandler<T> - Event Handler Interface
```csharp
public interface IIntegrationEventHandler<in TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    Task Handle(TIntegrationEvent @event, CancellationToken cancellationToken);
}
```

## Usage

### Step 1: Define Events (in the Abstractions project)
```csharp
// Senparc.Xncf.PromptRange.Abstractions/Events/PromptOptimizationEvents.cs
public record PromptOptimizationRequestEvent(
    string RequestId,
    string PromptCode,
    string UserRequirement
) : IntegrationEvent;

public record PromptOptimizationResponseEvent(
    string RequestId,
    string NewPromptCode,
    double Score,
    string EvaluationReason
) : IntegrationEvent;
```

### Step 2: Implement Event Handlers
```csharp
// Senparc.Xncf.AgentsManager/Application/EventHandlers/PromptOptimizationRequestHandler.cs
public class PromptOptimizationRequestHandler : IIntegrationEventHandler<PromptOptimizationRequestEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<PromptOptimizationRequestHandler> _logger;

    public PromptOptimizationRequestHandler(IEventBus eventBus, ILogger<PromptOptimizationRequestHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(PromptOptimizationRequestEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received Prompt optimization request: {RequestId}", @event.RequestId);

        // Execute business logic...
        string optimizedPrompt = await OptimizePromptAsync(@event.PromptCode, @event.UserRequirement);

        // Publish response event
        var response = new PromptOptimizationResponseEvent(
            @event.RequestId,
            optimizedPrompt,
            0.95,
            "Optimization successful"
        );
        
        await _eventBus.PublishAsync(response);
    }
}
```

### Step 3: Register EventBus and Handlers
```csharp
// Startup.cs or Program.cs
services.AddSenparcEventBus(
    options =>
    {
        options.MaxConcurrency = 20;                    // Max concurrency
        options.EnableDuplicateDetection = true;        // Enable duplicate detection
        options.RetryOnFailure = true;                  // Enable failure retry
        options.MaxRetryAttempts = 3;                   // Max retry attempts
    },
    typeof(PromptOptimizationRequestHandler).Assembly,  // Scan assemblies
    typeof(AgentCreatedHandler).Assembly
);
```

### Step 4: Publish Events
```csharp
public class MyService
{
    private readonly IEventBus _eventBus;

    public MyService(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task TriggerOptimizationAsync(string promptCode, string requirement)
    {
        var requestEvent = new PromptOptimizationRequestEvent(
            Guid.NewGuid().ToString(),
            promptCode,
            requirement
        );
        
        // Async publish (non-blocking, returns immediately)
        await _eventBus.PublishAsync(requestEvent);
    }
}
```

## Concurrency Configuration Recommendations

### Choose concurrency level based on scenario

1. **Low-latency scenarios (high real-time requirements)**
   ```csharp
   options.MaxConcurrency = Environment.ProcessorCount * 4;  // CPU cores * 4
   ```

2. **Database-intensive scenarios**
   ```csharp
   options.MaxConcurrency = DbConnectionPoolSize / 2;  // Half of DB connection pool
   ```

3. **External API call scenarios**
   ```csharp
   options.MaxConcurrency = 50;  // Set based on external API QPS limits
   ```

4. **Mixed scenarios (recommended)**
   ```csharp
   options.MaxConcurrency = Math.Max(8, Environment.ProcessorCount * 2);
   ```

## Duplicate Processing Prevention

EventBus automatically tracks event IDs processed within the last 10 minutes:

```csharp
// Implemented in InMemoryEventBus
public bool TryMarkEventAsProcessed(Guid eventId)
{
    return _processedEventIds.TryAdd(eventId, DateTime.UtcNow);
}
```

Features:
- Each event has a unique `Guid Id`
- 10-minute sliding window (configurable)
- Automatic cleanup of expired records (triggered every 100 calls)
- Thread-safe (uses `ConcurrentDictionary`)

## Failure Retry Mechanism

Uses **exponential backoff strategy**:
- 1st retry: delay 2^0 = 1 second
- 2nd retry: delay 2^1 = 2 seconds
- 3rd retry: delay 2^2 = 4 seconds

```csharp
// Configure retry
options.RetryOnFailure = true;
options.MaxRetryAttempts = 3;
```

## Cross-Module Communication Best Practices

### Problem: AgentsManager and PromptRange have circular dependencies

```
AgentsManager --calls--> PromptRange (optimize Prompt)
                  |
PromptRange --calls--> AgentsManager (use Agent for optimization)
```

### Solution: Decouple via EventBus

1. **Create a shared Abstractions project**
   ```
   Senparc.Xncf.PromptRange.Abstractions
   +-- Events/
   |   +-- PromptOptimizationRequestEvent.cs
   |   +-- PromptOptimizationResponseEvent.cs
   ```

2. **AgentsManager publishes request event**
   ```csharp
   await _eventBus.PublishAsync(new PromptOptimizationRequestEvent(...));
   ```

3. **PromptRange handles request and publishes response**
   ```csharp
   public class PromptOptimizationRequestHandler : IIntegrationEventHandler<PromptOptimizationRequestEvent>
   {
       public async Task Handle(PromptOptimizationRequestEvent @event, CancellationToken ct)
       {
           // Handle optimization logic...
           await _eventBus.PublishAsync(new PromptOptimizationResponseEvent(...));
       }
   }
   ```

4. **AgentsManager receives response**
   ```csharp
   public class PromptOptimizationResponseHandler : IIntegrationEventHandler<PromptOptimizationResponseEvent>
   {
       public Task Handle(PromptOptimizationResponseEvent @event, CancellationToken ct)
       {
           // Handle response...
       }
   }
   ```

## Request-Response Pattern Implementation

For scenarios requiring a response, use `TaskCompletionSource`:

```csharp
public class PromptOptimizationService
{
    private readonly IEventBus _eventBus;
    private static readonly ConcurrentDictionary<string, TaskCompletionSource<PromptOptimizationResponseEvent>> 
        _pendingRequests = new();

    public async Task<PromptOptimizationResponseEvent> OptimizePromptAsync(string promptCode, string requirement)
    {
        var requestId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<PromptOptimizationResponseEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        _pendingRequests.TryAdd(requestId, tcs);

        // Publish request
        await _eventBus.PublishAsync(new PromptOptimizationRequestEvent(requestId, promptCode, requirement));

        // Wait for response (with timeout)
        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            _pendingRequests.TryRemove(requestId, out _);
            throw new TimeoutException("Prompt optimization request timed out");
        }

        return await tcs.Task;
    }

    // Response Handler calls this method to complete the request
    public void CompleteRequest(string requestId, PromptOptimizationResponseEvent response)
    {
        if (_pendingRequests.TryRemove(requestId, out var tcs))
        {
            tcs.TrySetResult(response);
        }
    }
}
```

## Performance Monitoring

EventBus automatically logs key metrics:
- Event processing time
- Handler execution time
- Retry count
- Duplicate event count

View logs:
```csharp
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Senparc.Ncf.Core.EventBus": "Information"  // Or "Debug" for detailed info
    }
  }
}
```

## FAQ

### Q1: How to ensure event processing order?
A: EventBus does not guarantee global ordering, but events of the same type are processed in FIFO order. For strict ordering, consider:
   - Setting `MaxConcurrency` to 1 (serial processing)
   - Or using version numbers/sequence numbers in the business layer

### Q2: What if events are lost?
A: The current implementation uses an in-memory queue; events are lost on application restart. For production environments, consider:
   - Using a persistent message queue (RabbitMQ, Azure Service Bus)
   - Or implementing a persistent version of `IEventBus`

### Q3: How to handle transactional consistency?
A: Use the Outbox Pattern:
   ```csharp
   using var transaction = await _dbContext.Database.BeginTransactionAsync();
   
   // 1. Save business data
   await _dbContext.SaveChangesAsync();
   
   // 2. Save event to Outbox table
   await _outboxRepository.AddAsync(new OutboxMessage { EventData = ... });
   
   // 3. Commit transaction
   await transaction.CommitAsync();
   
   // 4. Background task publishes events from Outbox
   ```

### Q4: What concurrency level is appropriate?
A: Rule of thumb:
   ```
   MaxConcurrency = min(
       CPU cores * 2,
       DB connection pool size / 2,
       External API concurrency limit
   )
   ```

## Changelog

### v0.23.17 (2025-02-15)
- Added high-concurrency support (configurable concurrency level)
- Added duplicate event detection mechanism
- Added automatic failure retry (exponential backoff)
- Added detailed performance monitoring logs
- Refactored EventBusHostedService for async concurrent processing
- Fixed performance bottleneck caused by serial processing

## References

- [System.Threading.Channels Documentation](https://docs.microsoft.com/en-us/dotnet/api/system.threading.channels)
- [Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)
- [Event-Driven Architecture](https://martinfowler.com/articles/201701-event-driven.html)
