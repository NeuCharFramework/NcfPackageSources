# Step S7 - Jupyter Reverse Proxy / Authentication

## Goal
Access the interactive Notebook through the site's reverse proxy, require NCF Admin authentication, and inject the Jupyter token only on the server.

## Key Changes
- `Domain/Services/SandboxJupyterPaths.cs` — path conventions
- `OHS/Local/Middleware/SandboxJupyterProxyMiddleware.cs` — HTTP + WebSocket proxy
- `Domain/Services/Runtime/DockerSandboxRuntime.cs` — `ServerApp.base_url` + token-free AccessUrl
- `Domain/Services/SandboxOrchestrator.cs` — `TryGetJupyterProxyTargetAsync`
- `Register.cs` — `UseWebSockets` + middleware + HttpClient

## Verification
1. An unauthenticated request to `/sandbox-jupyter/{id}/lab` redirects to login.
2. A signed-in user with a Running session can open Lab and use the kernel WebSocket.
3. AccessUrl / Function output contains no `token=`.
4. `SandboxJupyterPathsTests` passes.
