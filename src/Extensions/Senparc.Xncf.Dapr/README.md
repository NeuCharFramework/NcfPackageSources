# Senparc.Xncf.Dapr

`Senparc.Xncf.Dapr` provides an NCF-friendly client abstraction for Dapr service invocation, pub/sub, state management, and health checks.

## Features

- Invokes Dapr services with GET, POST, PUT, PATCH, and DELETE semantics.
- Publishes events through a configured pub/sub component.
- Reads, writes, and deletes Dapr state values.
- Provides serializer abstractions and tolerant JSON converters for common scalar types.
- Registers as an XNCF module and uses `DaprClientOptions` for sidecar/component settings.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.Dapr" Version="0.26.0-preview3" />
```

## Key API

- `DaprClientServiceCollectionExtensions.AddDaprClient(...)` registers the client and optional `DaprClientOptions` configuration.
- `DaprClient.InvokeMethodAsync<TResult>(...)` is the generic service-invocation entry point; `GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, and `DeleteAsync` are convenience methods.
- `PublishEventAsync(...)` publishes an event to a configured topic.
- `SetStatesAsync(...)` and `DelStateAsync(...)` manage state; `HealthCheckAsync()` checks the Dapr path.
- `ISerializer` and `TextJsonConverter` define serialization behavior.

Run and secure the Dapr sidecar separately. Configure service IDs, component names, timeouts, network policy, and authentication in the deployment environment; do not treat a reachable sidecar as an authorization boundary.
