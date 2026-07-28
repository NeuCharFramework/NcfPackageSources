# Senparc.Xncf.MCP

`Senparc.Xncf.MCP` is an NCF module for registering, testing, and managing Model Context Protocol (MCP) endpoints and tools.

## Features

- Stores MCP endpoint configuration and exposes management application services.
- Supports endpoint create/edit/delete, connectivity tests, and tool invocation request models.
- Provides `NcfMcpTools` and `McpServerService` integration points for host-side MCP calls.
- Uses NCF module resources, authorization areas, and multi-database context conventions.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.MCP" Version="0.26.0-preview3" />
```

## Key API

- `MCPEndpointService` and `MCPEndpointAppService` manage endpoint records.
- `MCPEndpointCreateOrEditRequest`, `MCPEndpointDeleteRequest`, and `MCPEndpointTestRequest` model endpoint operations.
- `McpServerService` works with `McpServerData` for runtime server information.
- `NcfMcpTools` exposes the module's NCF/MCP tool bridge.
- `Register` and `McpResource` integrate module registration and localized metadata.

MCP endpoints are executable integration boundaries. Store credentials securely, allowlist endpoint URLs and tools, enforce tenant/admin authorization, and apply timeouts and audit logging before exposing them to users.
