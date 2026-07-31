# Senparc.Xncf.DesktopBridge

`Senparc.Xncf.DesktopBridge` is an optional XNCF module that exposes a secured HTTP/SSE bridge between an NCF host and a desktop companion application. The NCF host still uses its in-process EventBus; the bridge only adapts normalized activity and authorized resource-change notifications for another process or machine.

## Features

- Observes integration events through closed, contravariant handler mappings without replacing existing EventBus consumers.
- Provides capability discovery, activity snapshots, and Server-Sent Events (SSE) for local or explicitly configured remote desktop sessions.
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

## Remote deployment

Set `NCF_DESKTOP_BRIDGE_TOKEN` in the remote `Senparc.Web` process and enter the same value in the desktop workspace. Do not expose the bridge over plaintext Internet HTTP: use HTTPS and a reverse proxy/firewall IP allowlist. If mTLS is required, terminate it in a trusted local proxy/tunnel and connect the desktop workspace to that loopback endpoint. An SSH tunnel is likewise supported through a URL such as `http://127.0.0.1:5500`.

The token is an application-layer shared secret, not a replacement for TLS or network access control. Each desktop workspace owns an independent SSE connection and Admin JWT; the bridge never attempts to share the in-memory EventBus channel across processes.
