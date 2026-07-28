# Senparc.Xncf.DesktopBridge

`Senparc.Xncf.DesktopBridge` is an optional XNCF module that exposes a secured local bridge between an NCF host and a desktop companion application.

## Features

- Observes integration events through closed, contravariant handler mappings without replacing existing EventBus consumers.
- Provides capability discovery, activity snapshots, and Server-Sent Events (SSE) for a local desktop session.
- Requires the `NCF_DESKTOP_BRIDGE_TOKEN` startup boundary before exposing bridge state.
- Provides an administrator-scoped authorized-sync stream for resource IDs and change types.
- Does not read or mutate business `MemoryCache`, and does not transmit passwords, JWTs, or chat message bodies through EventBus/SSE.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.DesktopBridge" Version="0.1.1-preview3" />
```

Restart the NCF host after installing or updating the module.

## Key API and security contract

- `DesktopBridgeController` serves capability and activity endpoints.
- `DesktopActivityHub` and `DesktopAuthorizedSyncHub` publish lightweight activity/change messages.
- `DesktopBridgeTokenValidator` validates the local session token.
- `DesktopActivityEventHandler` observes NCF integration events.
- `DesktopBridgeCapabilities`, `DesktopActivityMessage`, and `DesktopAuthorizedSyncMessage` are the public transport records.

Authorized sync requires the desktop session token, the `Bearer_Backend` JWT, the standard `AdminOnly` policy, and matching event owner/admin IDs. The desktop client must re-read business data through the original authorized API; a missing login, expired token, or disconnected bridge must disable the dependent feature.
