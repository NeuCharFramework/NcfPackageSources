# Senparc NCF EventBus - High concurrency event bus

## Overview

based on`System.Threading.Channels.Channel`Implemented high-performance memory event bus, supporting:
- ✅ High concurrency event processing (configurable concurrency)
- ✅ Duplicate event detection (prevent the same event from being processed multiple times)
- ✅ Automatic retry on failure (exponential backoff strategy)
- ✅ Decoupled communication across modules (avoiding circular dependencies)
- ✅ Asynchronous non-blocking publishing (the publisher will not wait for processing to complete)

## Core components

### 1. IEventBus - event bus interface
```csharp
public interface IEventBus
{
    ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
```

### 2. IntegrationEvent - event base class
```csharp
public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid Id { get; } = Guid.NewGuid();  // 自动生成唯一 ID
    public DateTime CreationDate { get; } = DateTime.UtcNow;
}
```

### 3. IIntegrationEventHandler<T> - event handler interface
```csharp
public interface IIntegrationEventHandler<in TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    Task Handle(TIntegrationEvent @event, CancellationToken cancellationToken);
}
```

## How to use

### Step 1: Define events (in the Abstractions project)
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

### Step 2: Implement event handler
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
        _logger.LogInformation("收到 Prompt 优化请求: {RequestId}", @event.RequestId);

        // 执行业务逻辑...
        string optimizedPrompt = await OptimizePromptAsync(@event.PromptCode, @event.UserRequirement);

        // 发布响应事件
        var response = new PromptOptimizationResponseEvent(
            @event.RequestId,
            optimizedPrompt,
            0.95,
            "优化成功"
        );
        
        await _eventBus.PublishAsync(response);
    }
}
```

### Step 3: Register EventBus and Handler
```csharp
// Startup.cs 或 Program.cs
services.AddSenparcEventBus(
    options =>
    {
        options.MaxConcurrency = 20;                    // 最大并发数
        options.EnableDuplicateDetection = true;        // 启用重复检测
        options.RetryOnFailure = true;                  // 启用失败重试
        options.MaxRetryAttempts = 3;                   // 最大重试次数
    },
    typeof(PromptOptimizationRequestHandler).Assembly,  // 扫描程序集
    typeof(AgentCreatedHandler).Assembly
);
```

### Step 4: Publish the event
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
        
        // 异步发布（不会阻塞，立即返回）
        await _eventBus.PublishAsync(requestEvent);
    }
}
```

## High concurrency configuration recommendations

### Select the concurrency degree according to the scenario

1. **Low latency scenario (high real-time requirements)**
   ```csharp
options.MaxConcurrency = Environment.ProcessorCount * 4; // Number of CPU cores * 4
   ```

2. **Database intensive scenarios**
   ```csharp
options.MaxConcurrency = DbConnectionPoolSize / 2; // Half of the database connection pool
   ```

3. **External API calling scenario**
   ```csharp
options.MaxConcurrency = 50; // QPS limit settings based on external API
   ```

4. **Mixed Scenario (Recommended)**
   ```csharp
   options.MaxConcurrency = Math.Max(8, Environment.ProcessorCount * 2);
   ```

## Prevent duplicate processing mechanism

EventBus automatically tracks event IDs processed in the last 10 minutes:

```csharp
// 在 InMemoryEventBus 中实现
public bool TryMarkEventAsProcessed(Guid eventId)
{
    return _processedEventIds.TryAdd(eventId, DateTime.UtcNow);
}
```

Features:
- Each event has a unique`Guid Id`
- 10 minute sliding window (configurable)
- Automatically clean up expired records (triggered every 100 calls)
- Thread safe (use`ConcurrentDictionary`）

## Failure retry mechanism

Use **exponential backoff strategy**:
- 1st retry: delay 2^0 = 1 second
- 2nd retry: delay 2^1 = 2 seconds
- 3rd retry: delay 2^2 = 4 seconds

```csharp
// 配置重试
options.RetryOnFailure = true;
options.MaxRetryAttempts = 3;
```

## Best practices for cross-module communication

### Problem: AgentsManager and PromptRange depend on each other

```
AgentsManager ──调用──> PromptRange (优化 Prompt)
                  ↓
PromptRange ──调用──> AgentsManager (使用 Agent 优化)
```

### Solution: Decoupling via EventBus

1. **Create a shared Abstractions project**
   ```
   Senparc.Xncf.PromptRange.Abstractions
   ├── Events/
   │   ├── PromptOptimizationRequestEvent.cs
   │   └── PromptOptimizationResponseEvent.cs
   ```

2. **AgentsManager publishes request events**
   ```csharp
   await _eventBus.PublishAsync(new PromptOptimizationRequestEvent(...));
   ```

3. **PromptRange processes the request and publishes the response**
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

## Request-response pattern implementation

For scenarios where you need to wait for a response, use`TaskCompletionSource`：

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

        // 发布请求
        await _eventBus.PublishAsync(new PromptOptimizationRequestEvent(requestId, promptCode, requirement));

        // 等待响应（带超时）
        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            _pendingRequests.TryRemove(requestId, out _);
            throw new TimeoutException("Prompt 优化请求超时");
        }

        return await tcs.Task;
    }

    // 响应 Handler 调用此方法完成请求
    public void CompleteRequest(string requestId, PromptOptimizationResponseEvent response)
    {
        if (_pendingRequests.TryRemove(requestId, out var tcs))
        {
            tcs.TrySetResult(response);
        }
    }
}
```

## Performance monitoring

EventBus automatically records key metrics:
- event processing time
- Handler execution time
-Number of retries
- Number of duplicate events

Check the log:
```csharp
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Senparc.Ncf.Core.EventBus": "Information"  // 或 "Debug" 查看详细信息
    }
  }
}
```

## FAQ

### Q1: How to ensure the order of event processing?
A: EventBus does not guarantee global order, but events of the same type will be processed in FIFO order. For strict order, consider:
- Will`MaxConcurrency`Set to 1 (serial processing)
- Or use version number/serial number in business layer

### Q2: What should I do if the event is lost?
A: The current implementation is a memory queue, which will be lost when the application restarts. Recommendations for production environments:
- Use persistent message queue (RabbitMQ, Azure Service Bus)
- or implement`IEventBus`persistent version of

### Q3: How to deal with transaction consistency?
A: Use Outbox Pattern:
   ```csharp
   using var transaction = await _dbContext.Database.BeginTransactionAsync();
   
// 1. Save business data
   await _dbContext.SaveChangesAsync();
   
// 2. Save the event to the Outbox table
   await _outboxRepository.AddAsync(new OutboxMessage { EventData = ... });
   
// 3. Submit transaction
   await transaction.CommitAsync();
   
// 4. The background task publishes events from Outbox
   ```

### Q4: What is the appropriate concurrency setting?
A: Empirical formula:
   ```
   MaxConcurrency = min(
Number of CPU cores * 2,
Database connection pool size / 2,
External API concurrency limits
   )
   ```

## Update log

### v0.23.17 (2025-02-15)
- ✨ Added high concurrency support (configurable concurrency)
- ✨ Added duplicate event detection mechanism
- ✨ Added automatic retry on failure (exponential backoff)
- ✨ Added detailed performance monitoring logs
- ♻️ Refactor EventBusHostedService to support asynchronous concurrent processing
- 🐛 Fix performance bottleneck caused by serial processing

## References

- [System.Threading.Channels official documentation](https://docs.microsoft.com/en-us/dotnet/api/system.threading.channels)
- [Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)
- [Event-Driven Architecture](https://martinfowler.com/articles/201701-event-driven.html)
