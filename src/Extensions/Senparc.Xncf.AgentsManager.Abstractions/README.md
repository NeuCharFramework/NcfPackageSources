# Senparc.Xncf.AgentsManager.Abstractions

`Senparc.Xncf.AgentsManager.Abstractions` is the lightweight dependency boundary for components that integrate with the NCF Agents Manager event and shared-contract layer.

## Features

- Keeps shared event contracts separate from the Agents Manager UI and persistence module.
- References NCF shared abstractions for integration-event composition.
- Lets optional modules depend on the contract package without loading agent-management implementation details.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.AgentsManager.Abstractions" Version="0.2.2-preview2" />
```

## Key API

Use the shared event and integration contracts exposed through the package dependency when defining an optional agent integration. The package does not create agent templates, execute tools, persist chat history, or configure an AI provider; use `Senparc.Xncf.AgentsManager` for those runtime features.

Treat agent messages and tool calls as sensitive data and enforce ownership and authorization in the consuming module.
