[中文版](EVENTBUS_QUICK_REFERENCE.cn.md)

# EventBus Circular Reference Guard - Quick Reference

## 🚀 Quick Start

### 1. Publish derived events in Handler
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
### 2. Configure protection options
```csharp
services.AddSenparcEventBus(options =>
{
    options.MaxEventChainDepth = 10;                      // 最大深度
    options.EnableCircularReferenceDetection = true;      // 循环检测
}, 
typeof(YourAssembly).Assembly);
```
---

## 🛡️ Three-layer protection mechanism

### First level: Depth limit
```
检查时机: 运行时（处理事件前）
检查逻辑: event.Depth >= MaxEventChainDepth
处理方式: 丢弃事件 + 记录错误日志
默认限制: 10 层
```
### Second level: loop detection
```
检查时机: 运行时（处理事件前）
检查逻辑: event.EventChain.Contains(currentEventType)
处理方式: 丢弃事件 + 记录错误日志
检测模式: A→B→A, A→A
```
### The third level: pre-release
```
检查时机: 发布时（PublishDerivedAsync）
检查逻辑: parentEvent.HasCircularReference(newEventType)
处理方式: 抛出 InvalidOperationException
优势: 提前阻止，避免进入队列
```
---

## 📊 Event chain example

### Normal process
```
Request Event (Depth=0, Chain="")
  ↓
Response Event (Depth=1, Chain="RequestEvent")
  ↓
Complete (不再发布事件)
✅ 安全
```
### Loop scene (blocked)
```
Event A (Depth=0, Chain="")
  ↓
Event B (Depth=1, Chain="EventA")
  ↓
Event A (Depth=2, Chain="EventA→EventB")
  ❌ 检测到循环：EventA 已在链中
  🛑 抛出异常或丢弃事件
```
### Depth exceeded (blocked)
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

## 🔍 Log example

### Normal processing
```
[Information] Processing event MyRequestEvent (Id: xxx, Depth: 0, Chain: )
[Debug] Publishing derived event: MyResponseEvent (ParentId: xxx, Depth: 1, Chain: MyRequestEvent)
[Information] Event MyResponseEvent processed successfully in 15ms
```
### Loop detected
```
[Error] Circular reference detected: MyRequestEvent 
        (Id: xxx, Chain: MyRequestEvent→MyResponseEvent→MyRequestEvent)
```
### Depth exceeds limit
```
[Error] Event chain depth limit exceeded: MyEvent 
        (Id: xxx, Depth: 10, Chain: Event1→Event2→...→Event10)
```
---

## ✅ Checklist

When implementing Handler, make sure:

- [ ] event definition inherited from `IntegrationEvent` base class
- [ ] Use `PublishDerivedAsync` to publish derived events
- [ ] Avoid publishing request events in response Handler
- [ ] configured with `EnableCircularReferenceDetection = true`
- [ ] Set a reasonable `MaxEventChainDepth` (recommendation 10)
- [ ] Added adequate logging
- [ ] Wrote unit tests to verify event flow

---

## 🐛 FAQ

### Q1: InvalidOperationException - Circular reference detected

**Cause**: There are duplicate types in the event chain
**Solution**: Check the EventChain in the log to avoid circular dependencies

### Q2: Event chain depth limit exceeded

**Cause**: The event nesting level is too deep
**Solution**: Refactor to a flatter structure, or increase MaxEventChainDepth

### Q3: ArgumentException - Event must inherit from IntegrationEvent

**Cause**: The event type does not inherit the IntegrationEvent base class
**Solution**: Modify the event definition as `public record MyEvent(...) : IntegrationEvent`

---

## 📚 Related documents

- **Technical Detailed Document**: [EVENTBUS_CIRCULAR_REFERENCE_PROTECTION.md](./EVENTBUS_CIRCULAR_REFERENCE_PROTECTION.md)
- **Flowchart and Architecture**: [EVENTBUS_FLOW_DIAGRAMS.md](./EVENTBUS_FLOW_DIAGRAMS.md)
- **Full inspection report**: [EVENTBUS_COMPLETE_SUMMARY.md](./EVENTBUS_COMPLETE_SUMMARY.md)

---

## 🎯 Core Principles

1. **Safety First**: Enable all protection mechanisms
2. **Performance first**: Use non-blocking API
3. **Observability**: Record detailed logs
4. **Backwards Compatibility**: Smooth upgrade and painless migration

---

**Version**: 1.0
**Update**: 2026-03-24
