# Senparc NCF EventBus - 高并发事件总线

## 概述

基于 `System.Threading.Channels.Channel` 实现的高性能内存事件总线，支持：
- ✅ 高并发事件处理（可配置并发度）
- ✅ 重复事件检测（防止同一事件被处理多次）
- ✅ 失败自动重试（指数退避策略）
- ✅ 跨模块解耦通信（避免循环依赖）
- ✅ 异步非阻塞发布（发布者不会等待处理完成）

## 核心组件

### 1. IEventBus - 事件总线接口
```csharp
public interface IEventBus
{
    ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
```

### 2. IntegrationEvent - 事件基类
```csharp
public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid Id { get; } = Guid.NewGuid();  // 自动生成唯一 ID
    public DateTime CreationDate { get; } = DateTime.UtcNow;
}
```

### 3. IIntegrationEventHandler<T> - 事件处理器接口
```csharp
public interface IIntegrationEventHandler<in TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    Task Handle(TIntegrationEvent @event, CancellationToken cancellationToken);
}
```

## 使用方法

### 步骤 1: 定义事件（在 Abstractions 项目中）
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

### 步骤 2: 实现事件处理器
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

### 步骤 3: 注册 EventBus 和 Handler
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

### 步骤 4: 发布事件
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

## 高并发配置建议

### 根据场景选择并发度

1. **低延迟场景（实时性要求高）**
   ```csharp
   options.MaxConcurrency = Environment.ProcessorCount * 4;  // CPU 核心数 * 4
   ```

2. **数据库密集型场景**
   ```csharp
   options.MaxConcurrency = DbConnectionPoolSize / 2;  // 数据库连接池的一半
   ```

3. **外部 API 调用场景**
   ```csharp
   options.MaxConcurrency = 50;  // 根据外部 API 的 QPS 限制设置
   ```

4. **混合场景（推荐）**
   ```csharp
   options.MaxConcurrency = Math.Max(8, Environment.ProcessorCount * 2);
   ```

## 防止重复处理机制

EventBus 自动追踪最近 10 分钟处理过的事件 ID：

```csharp
// 在 InMemoryEventBus 中实现
public bool TryMarkEventAsProcessed(Guid eventId)
{
    return _processedEventIds.TryAdd(eventId, DateTime.UtcNow);
}
```

特点：
- 每个事件都有唯一的 `Guid Id`
- 10 分钟滑动窗口（可配置）
- 自动清理过期记录（每 100 次调用触发一次）
- 线程安全（使用 `ConcurrentDictionary`）

## 失败重试机制

采用**指数退避策略**：
- 第 1 次重试：延迟 2^0 = 1 秒
- 第 2 次重试：延迟 2^1 = 2 秒
- 第 3 次重试：延迟 2^2 = 4 秒

```csharp
// 配置重试
options.RetryOnFailure = true;
options.MaxRetryAttempts = 3;
```

## 跨模块通信最佳实践

### 问题：AgentsManager 和 PromptRange 相互依赖

```
AgentsManager ──调用──> PromptRange (优化 Prompt)
                  ↓
PromptRange ──调用──> AgentsManager (使用 Agent 优化)
```

### 解决方案：通过 EventBus 解耦

1. **创建共享的 Abstractions 项目**
   ```
   Senparc.Xncf.PromptRange.Abstractions
   ├── Events/
   │   ├── PromptOptimizationRequestEvent.cs
   │   └── PromptOptimizationResponseEvent.cs
   ```

2. **AgentsManager 发布请求事件**
   ```csharp
   await _eventBus.PublishAsync(new PromptOptimizationRequestEvent(...));
   ```

3. **PromptRange 处理请求并发布响应**
   ```csharp
   public class PromptOptimizationRequestHandler : IIntegrationEventHandler<PromptOptimizationRequestEvent>
   {
       public async Task Handle(PromptOptimizationRequestEvent @event, CancellationToken ct)
       {
           // 处理优化逻辑...
           await _eventBus.PublishAsync(new PromptOptimizationResponseEvent(...));
       }
   }
   ```

4. **AgentsManager 接收响应**
   ```csharp
   public class PromptOptimizationResponseHandler : IIntegrationEventHandler<PromptOptimizationResponseEvent>
   {
       public Task Handle(PromptOptimizationResponseEvent @event, CancellationToken ct)
       {
           // 处理响应...
       }
   }
   ```

## 请求-响应模式实现

对于需要等待响应的场景，使用 `TaskCompletionSource`：

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

## 性能监控

EventBus 自动记录关键指标：
- 事件处理时间
- Handler 执行时间
- 重试次数
- 重复事件数量

查看日志：
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

## 常见问题

### Q1: 如何确保事件处理的顺序？
A: EventBus 不保证全局顺序，但同一类型的事件会按 FIFO 顺序处理。如需严格顺序，考虑：
   - 将 `MaxConcurrency` 设为 1（串行处理）
   - 或在业务层使用版本号/序列号

### Q2: 事件丢失怎么办？
A: 当前实现是内存队列，应用重启会丢失。生产环境建议：
   - 使用持久化消息队列（RabbitMQ, Azure Service Bus）
   - 或实现 `IEventBus` 的持久化版本

### Q3: 如何处理事务一致性？
A: 采用 Outbox Pattern：
   ```csharp
   using var transaction = await _dbContext.Database.BeginTransactionAsync();
   
   // 1. 保存业务数据
   await _dbContext.SaveChangesAsync();
   
   // 2. 保存事件到 Outbox 表
   await _outboxRepository.AddAsync(new OutboxMessage { EventData = ... });
   
   // 3. 提交事务
   await transaction.CommitAsync();
   
   // 4. 后台任务从 Outbox 发布事件
   ```

### Q4: 并发度设置多少合适？
A: 经验公式：
   ```
   MaxConcurrency = min(
       CPU 核心数 * 2,
       数据库连接池大小 / 2,
       外部 API 并发限制
   )
   ```

## 更新日志

### v0.23.17 (2025-02-15)
- ✨ 新增高并发支持（可配置并发度）
- ✨ 新增重复事件检测机制
- ✨ 新增失败自动重试（指数退避）
- ✨ 新增详细的性能监控日志
- ♻️ 重构 EventBusHostedService，支持异步并发处理
- 🐛 修复串行处理导致的性能瓶颈

## 参考资料

- [System.Threading.Channels 官方文档](https://docs.microsoft.com/en-us/dotnet/api/system.threading.channels)
- [Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)
- [Event-Driven Architecture](https://martinfowler.com/articles/201701-event-driven.html)
