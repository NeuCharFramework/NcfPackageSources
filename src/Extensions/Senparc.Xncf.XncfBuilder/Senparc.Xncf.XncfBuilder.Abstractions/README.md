# Senparc.Xncf.XncfBuilder.Abstractions

`Senparc.Xncf.XncfBuilder.Abstractions` contains the cross-module contracts used by XNCF Builder's module-inventory exchange.

## Features

- Immutable inventory item and request/response event records.
- A waiter abstraction for correlating an inventory request with its asynchronous response.
- No dependency on the builder UI or generated module implementation.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.XncfBuilder.Abstractions" Version="0.26.0-preview3" />
```

## Key API

- `XncfModuleInventoryItem` describes an installed or available module.
- `XncfModulesInventoryRequestEvent` starts an inventory request.
- `XncfModulesInventoryResponseEvent` carries installed and not-installed module results.
- `IXncfModulesInventoryRequestWaiter.RegisterRequest(...)`, `TrySetResult(...)`, and `WaitForResponseAsync(...)` manage correlation and timeout handling.

Use the contracts with a compatible NCF `IEventBus`. Keep request IDs unique, enforce caller authorization before returning inventory, and do not expose filesystem paths or secrets in inventory data.
