# PromptRange EventBus 流程图

## 🔄 Prompt 初始化流程

```mermaid
sequenceDiagram
    participant User
    participant Service as PromptOptimizationService
    participant EventBus
    participant Handler1 as PromptInitRequestHandler
    participant Handler2 as PromptInitResponseHandler
    participant DB as Database

    User->>Service: EnsureInitializedAsync(modelId)
    Service->>Service: 检查 Agent 是否存在
    
    alt Agent 不存在
        Service->>EventBus: PublishAsync(PromptInitRequestEvent)<br/>Depth=0, Chain=""
        Note over EventBus: 深度检查 (0 < 10) ✅<br/>循环检查 (无) ✅
        EventBus->>Handler1: Handle(PromptInitRequestEvent)
        Handler1->>DB: 创建 PromptRange/PromptItem
        Handler1->>EventBus: PublishDerivedAsync(PromptInitResponseEvent)<br/>Depth=1, Chain="PromptInitRequestEvent"
        Note over EventBus: 深度检查 (1 < 10) ✅<br/>循环检查 (无重复) ✅
        EventBus->>Handler2: Handle(PromptInitResponseEvent)
        Handler2->>Service: CompleteInitRequest(TCS.SetResult)
        Service-->>User: 返回 PromptCode
    else Agent 已存在
        Service-->>User: 返回现有 PromptCode
    end
```

---

## 🎨 Prompt 优化流程

```mermaid
sequenceDiagram
    participant User
    participant Service as PromptOptimizationService
    participant EventBus
    participant Handler1 as PromptOptimizationRequestHandler
    participant Handler2 as PromptOptimizationResponseHandler

    User->>Service: OptimizePromptAsync(promptCode, content, requirement)
    Service->>EventBus: PublishAsync(PromptOptimizationRequestEvent)<br/>Depth=0, Chain=""
    Note over EventBus: 🛡️ 检查通过
    EventBus->>Handler1: Handle(PromptOptimizationRequestEvent)
    Handler1->>Handler1: 生成优化后的 Prompt
    Handler1->>EventBus: PublishDerivedAsync(PromptOptimizationResponseEvent)<br/>Depth=1, Chain="PromptOptimizationRequestEvent"
    Note over EventBus: 🛡️ 检查通过
    EventBus->>Handler2: Handle(PromptOptimizationResponseEvent)
    Handler2->>Service: CompleteRequest(TCS.SetResult)
    Service-->>User: 返回优化结果
```

---

## 🛡️ 循环引用防护示例

### 场景 1: 阻止直接循环

```mermaid
graph TD
    A[EventA<br/>Depth=0<br/>Chain=Empty] -->|Handler A| B[EventB<br/>Depth=1<br/>Chain=EventA]
    B -->|Handler B| C[EventA<br/>Depth=2<br/>Chain=EventA→EventB]
    C -->|❌ 检测到循环| D[丢弃事件<br/>记录错误日志]
    
    style D fill:#ff6b6b
```

### 场景 2: 阻止深度超限

```mermaid
graph TD
    A[Event 1<br/>Depth=0] --> B[Event 2<br/>Depth=1]
    B --> C[Event 3<br/>Depth=2]
    C --> D[Event 4<br/>Depth=3]
    D --> E[...]
    E --> F[Event 10<br/>Depth=9]
    F --> G[Event 11<br/>Depth=10]
    G -->|❌ 深度超限| H[丢弃事件<br/>记录错误日志]
    
    style G fill:#ffd93d
    style H fill:#ff6b6b
```

---

## 🔍 EventBus 核心架构

```mermaid
graph LR
    subgraph 发布端
        A[业务代码] -->|PublishAsync| B[InMemoryEventBus]
        A2[Handler] -->|PublishDerivedAsync| B
    end
    
    B -->|WriteAsync| C[Channel<br/>无界队列]
    
    subgraph 消费端
        C -->|ReadAllAsync| D[EventBusHostedService]
        D -->|1. 重复检测| E{已处理?}
        E -->|是| F[跳过]
        E -->|否| G{深度超限?}
        G -->|是| H[丢弃 + 日志]
        G -->|否| I{循环检测}
        I -->|是| J[丢弃 + 日志]
        I -->|否| K[SemaphoreSlim<br/>并发控制]
        K --> L[Task.Run<br/>异步处理]
        L --> M[Handler.Handle]
    end
    
    style F fill:#ffd93d
    style H fill:#ff6b6b
    style J fill:#ff6b6b
    style K fill:#6bcf7f
    style L fill:#6bcf7f
```

---

## 📈 性能特性

### 非阻塞发布

```mermaid
sequenceDiagram
    participant Caller
    participant EventBus
    participant Channel
    participant Background

    Caller->>EventBus: PublishAsync(event)
    EventBus->>Channel: WriteAsync(event)
    Channel-->>EventBus: ValueTask (立即返回)
    EventBus-->>Caller: ValueTask (< 1ms)
    
    Note over Background: 后台异步处理
    Background->>Channel: ReadAllAsync()
    Channel->>Background: 返回事件
    Background->>Background: 处理事件
```

### 并发控制

```mermaid
graph TD
    A[Channel 队列] --> B{SemaphoreSlim}
    B -->|获取信号| C1[Task 1]
    B -->|获取信号| C2[Task 2]
    B -->|获取信号| C3[Task 3]
    B -->|获取信号| CN[Task N]
    B -->|等待| W[等待队列]
    
    C1 --> D1[Handler]
    C2 --> D2[Handler]
    C3 --> D3[Handler]
    CN --> DN[Handler]
    
    D1 -->|释放信号| B
    D2 -->|释放信号| B
    D3 -->|释放信号| B
    DN -->|释放信号| B
    
    style B fill:#6bcf7f
    style W fill:#ffd93d
```

---

## 🎓 最佳实践总结

### ✅ 推荐模式

1. **请求-响应模式** (最安全)
   ```
   Request → Handler → Response → Complete
   ```

2. **单向事件流** (次安全)
   ```
   EventA → EventB → EventC → ... (不回溯)
   ```

3. **限制深度** (强制约束)
   ```
   最多 3-5 层事件嵌套
   ```

### ❌ 反模式

1. **响应再请求** (易循环)
   ```
   Request → Response → Request (❌ 循环风险)
   ```

2. **相互发布** (易循环)
   ```
   HandlerA publishes EventB
   HandlerB publishes EventA (❌ 循环风险)
   ```

3. **无限递归** (易爆栈)
   ```
   EventA → EventA → EventA → ... (❌ 深度风险)
   ```

---

**文档版本**: 1.0  
**最后更新**: 2026-03-24
