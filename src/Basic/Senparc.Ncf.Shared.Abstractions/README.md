# Senparc.Ncf.Shared.Abstractions

`Senparc.Ncf.Shared.Abstractions` contains small contracts shared by independently deployable NCF components. It is designed to reduce coupling between event producers, event consumers, and optional XNCF modules.

## Features

- Integration event and event-handler contracts.
- Request/response event contracts for in-process request clients.
- Authorized synchronization event markers for identity-scoped updates.
- NeuBell provider and change-notification contracts for optional Admin Footer monitoring.
- A narrow desktop admin token-issuer contract for server-side, one-time auth handoff; it does not expose browser cookies.
- No concrete web host or database implementation.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.Shared.Abstractions" Version="0.26.0-preview3" />
```

## Key API

- Implement `IIntegrationEvent` for a publishable event payload.
- Implement `IIntegrationEventHandler<TEvent>` for a consumer.
- Use `IIntegrationRequest` with `IEventBusRequestClient` for correlated request/response flows.
- Use `IAuthorizedIntegrationSyncEvent` when the event must be filtered by the current authorized owner.
- Implement `INeuBellProvider` in an XNCF module and register it with DI to expose a small, non-sensitive status snapshot.
- Implement `IDesktopAdminAuthTokenIssuer` only in the trusted Admin host; callers must first validate the desktop session, PKCE challenge, source identity, and source expiry.

The package defines contracts only. Register a compatible `IEventBus` implementation, validate event ownership in the consuming host, and avoid putting secrets or full message bodies into shared event payloads.
