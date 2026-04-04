# EventBus 循环引用防护 - 快速参考

## 🚀 快速开始

### 1. 在 Handler 中发布派生事件

```csharp
public class MyRequestHandler : IIntegrationEventHandler<MyRequestEvent>
{
    private readonly IEventBus _eventBus;

    public async Task Handle(MyRequestEvent @event, CancellationToken ct)
    {
        // ... 处理业务逻辑 ...
        
        var response = new MyResponseEvent(result);
        
        // ⭐ 使用 PublishDerivedAsync（自动继承链信息）
        await _eventBus.PublishDerivedAsync(response, @event);
    }
}
```

### 2. 配置防护选项

```csharp
services.AddSenparcEventBus(options =>
{
    options.MaxEventChainDepth = 10;                      // 最大深度
    options.EnableCircularReferenceDetection = true;      // 循环检测
}, 
typeof(YourAssembly).Assembly);
```

---

## 🛡️ 三层防护机制

### 第一层: 深度限制
```
检查时机: 运行时（处理事件前）
检查逻辑: event.Depth >= MaxEventChainDepth
处理方式: 丢弃事件 + 记录错误日志
默认限制: 10 层
```

### 第二层: 循环检测
```
检查时机: 运行时（处理事件前）
检查逻辑: event.EventChain.Contains(currentEventType)
处理方式: 丢弃事件 + 记录错误日志
检测模式: A→B→A, A→A
```

### 第三层: 发布前预检
```
检查时机: 发布时（PublishDerivedAsync）
检查逻辑: parentEvent.HasCircularReference(newEventType)
处理方式: 抛出 InvalidOperationException
优势: 提前阻止，避免进入队列
```

---

## 📊 事件链示例

### 正常流程
```
Request Event (Depth=0, Chain="")
  ↓
Response Event (Depth=1, Chain="RequestEvent")
  ↓
Complete (不再发布事件)
✅ 安全
```

### 循环场景（被阻止）
```
Event A (Depth=0, Chain="")
  ↓
Event B (Depth=1, Chain="EventA")
  ↓
Event A (Depth=2, Chain="EventA→EventB")
  ❌ 检测到循环：EventA 已在链中
  🛑 抛出异常或丢弃事件
```

### 深度超限（被阻止）
```
Event 1 (Depth=0)
  ↓
Event 2 (Depth=1)
  ↓
... (中间省略)
  ↓
Event 10 (Depth=9)
  ↓
Event 11 (Depth=10)
  ❌ 深度超限
  🛑 丢弃事件
```

---

## 🔍 日志示例

### 正常处理
```
[Information] Processing event MyRequestEvent (Id: xxx, Depth: 0, Chain: )
[Debug] Publishing derived event: MyResponseEvent (ParentId: xxx, Depth: 1, Chain: MyRequestEvent)
[Information] Event MyResponseEvent processed successfully in 15ms
```

### 检测到循环
```
[Error] Circular reference detected: MyRequestEvent 
        (Id: xxx, Chain: MyRequestEvent→MyResponseEvent→MyRequestEvent)
```

### 深度超限
```
[Error] Event chain depth limit exceeded: MyEvent 
        (Id: xxx, Depth: 10, Chain: Event1→Event2→...→Event10)
```

---

## ✅ 检查清单

在实现 Handler 时，确保：

- [ ] 事件定义继承自 `IntegrationEvent` 基类
- [ ] 使用 `PublishDerivedAsync` 发布派生事件
- [ ] 避免在响应 Handler 中发布请求事件
- [ ] 配置了 `EnableCircularReferenceDetection = true`
- [ ] 设置了合理的 `MaxEventChainDepth`（建议 10）
- [ ] 添加了充分的日志记录
- [ ] 编写了单元测试验证事件流

---

## 🐛 常见问题

### Q1: InvalidOperationException - Circular reference detected

**原因**: 事件链中存在重复类型  
**解决**: 检查日志中的 EventChain，避免循环依赖

### Q2: Event chain depth limit exceeded

**原因**: 事件嵌套层次过深  
**解决**: 重构为更扁平的结构，或增加 MaxEventChainDepth

### Q3: ArgumentException - Event must inherit from IntegrationEvent

**原因**: 事件类型未继承 IntegrationEvent 基类  
**解决**: 修改事件定义为 `public record MyEvent(...) : IntegrationEvent`

---

## 📚 相关文档

- **技术详细文档**: [EVENTBUS_CIRCULAR_REFERENCE_PROTECTION.md](./EVENTBUS_CIRCULAR_REFERENCE_PROTECTION.md)
- **流程图和架构**: [EVENTBUS_FLOW_DIAGRAMS.md](./EVENTBUS_FLOW_DIAGRAMS.md)
- **完整检查报告**: [EVENTBUS_COMPLETE_SUMMARY.md](./EVENTBUS_COMPLETE_SUMMARY.md)

---

## 🎯 核心原则

1. **安全第一**: 启用所有防护机制
2. **性能优先**: 使用非阻塞 API
3. **可观测性**: 记录详细日志
4. **向后兼容**: 平滑升级无痛迁移

---

**版本**: 1.0  
**更新**: 2026-03-24
