# EventBus Circular Reference Protection - Quick Reference

## 🚀 Quick Start

### 1. Publish derived events in handlers

```csharp
public class MyRequestHandler : IIntegrationEventHandler<MyRequestEvent>
{
    private readonly IEventBus _eventBus;

    public async Task Handle(MyRequestEvent @event, CancellationToken ct)
    {
        // ... handle business logic ...

        var response = new MyResponseEvent(result);

        // ⭐ Use PublishDerivedAsync (automatically carries event-chain metadata)
        await _eventBus.PublishDerivedAsync(response, @event);
    }
}
```

### 2. Configure protection options

```csharp
services.AddSenparcEventBus(options =>
{
    options.MaxEventChainDepth = 10;                      // Max depth
    options.EnableCircularReferenceDetection = true;      // Circular detection
},
typeof(YourAssembly).Assembly);
```

---

## 🛡️ Three-Layer Protection

### Layer 1: Depth limit
```
Check time: Runtime (before event handling)
Check logic: event.Depth >= MaxEventChainDepth
Action: Drop event + log error
Default limit: 10 levels
```

### Layer 2: Circular detection
```
Check time: Runtime (before event handling)
Check logic: event.EventChain.Contains(currentEventType)
Action: Drop event + log error
Patterns detected: A→B→A, A→A
```

### Layer 3: Pre-publish validation
```
Check time: Publish time (PublishDerivedAsync)
Check logic: parentEvent.HasCircularReference(newEventType)
Action: Throw InvalidOperationException
Benefit: Stops invalid events before they enter queue
```

---

## 📊 Event Chain Examples

### Normal flow
```
Request Event (Depth=0, Chain="")
  ↓
Response Event (Depth=1, Chain="RequestEvent")
  ↓
Complete (no further event publishing)
✅ Safe
```

### Circular scenario (blocked)
```
Event A (Depth=0, Chain="")
  ↓
Event B (Depth=1, Chain="EventA")
  ↓
Event A (Depth=2, Chain="EventA→EventB")
  ❌ Circular reference detected: EventA already exists in chain
  🛑 Exception thrown or event dropped
```

### Depth overflow (blocked)
```
Event 1 (Depth=0)
  ↓
Event 2 (Depth=1)
  ↓
... (omitted)
  ↓
Event 10 (Depth=9)
  ↓
Event 11 (Depth=10)
  ❌ Depth limit exceeded
  🛑 Event dropped
```

---

## 🔍 Log Examples

### Normal processing
```
[Information] Processing event MyRequestEvent (Id: xxx, Depth: 0, Chain: )
[Debug] Publishing derived event: MyResponseEvent (ParentId: xxx, Depth: 1, Chain: MyRequestEvent)
[Information] Event MyResponseEvent processed successfully in 15ms
```

### Circular reference detected
```
[Error] Circular reference detected: MyRequestEvent
        (Id: xxx, Chain: MyRequestEvent→MyResponseEvent→MyRequestEvent)
```

### Depth limit exceeded
```
[Error] Event chain depth limit exceeded: MyEvent
        (Id: xxx, Depth: 10, Chain: Event1→Event2→...→Event10)
```

---

## ✅ Checklist

When implementing handlers, ensure:

- [ ] Event definitions inherit from IntegrationEvent base class
- [ ] Use PublishDerivedAsync to publish derived events
- [ ] Avoid publishing request events from response handlers
- [ ] Configure EnableCircularReferenceDetection = true
- [ ] Set a reasonable MaxEventChainDepth (recommended 10)
- [ ] Add sufficient logging
- [ ] Add unit tests to validate event flows

---

## 🐛 Common Issues

### Q1: InvalidOperationException - Circular reference detected

**Cause**: Duplicate event type exists in event chain.
**Fix**: Inspect EventChain in logs and remove circular dependencies.

### Q2: Event chain depth limit exceeded

**Cause**: Event nesting is too deep.
**Fix**: Refactor to a flatter event architecture, or increase MaxEventChainDepth.

### Q3: ArgumentException - Event must inherit from IntegrationEvent

**Cause**: Event type does not inherit IntegrationEvent.
**Fix**: Define event as public record MyEvent(...) : IntegrationEvent.

---

## 📚 Related Docs

- Technical details: [EVENTBUS_CIRCULAR_REFERENCE_PROTECTION.md](./EVENTBUS_CIRCULAR_REFERENCE_PROTECTION.md)
- Flow diagrams and architecture: [EVENTBUS_FLOW_DIAGRAMS.md](./EVENTBUS_FLOW_DIAGRAMS.md)
- Full inspection report: [EVENTBUS_COMPLETE_SUMMARY.md](./EVENTBUS_COMPLETE_SUMMARY.md)

---

## 🎯 Core Principles

1. Safety first: enable all protection mechanisms.
2. Performance first: use non-blocking APIs.
3. Observability: log detailed diagnostics.
4. Backward compatibility: smooth upgrade with minimal migration cost.

---

**Version**: 1.0
**Updated**: 2026-03-24
