# MCP Endpoint Management

The admin page is `/Admin/MCP/Index?uid=149d8021-1783-4fc9-97a8-f1a1ba60245b`.
It supports listing, filtering, creating, editing, enabling/disabling and deleting endpoint configurations, as well as testing connections and inspecting tool schemas.

## API registration

All six management methods in `MCPEndpointAppService` use explicit POST `ApiBind` attributes and admin API authentication. Registering the service with dependency injection alone does **not** publish its methods as HTTP endpoints.

The route prefix is:

```text
/api/Senparc.Xncf.MCP/MCPEndpointAppService/Xncf.MCP_MCPEndpointAppService.
```

Methods: `GetAllEndpoints`, `GetEnabledEndpoints`, `SaveEndpoint`, `DeleteEndpoint`, `TestEndpoint`, and `TestConnection`.

After updating the module, rebuild and restart the host so that the dynamic API controllers and embedded JavaScript are regenerated. If an endpoint returns HTTP 404, verify that the host has loaded the updated module. HTTP 401/403 indicates missing authentication or insufficient permissions. The UI displays these errors instead of treating unexpected responses as successful saves.

## Endpoint addresses and connection tests

- Supply a unique name and the **complete** endpoint address. Leading/trailing whitespace is removed; paths and query strings are not rewritten.
- For the local NCF SSE server, use `http://localhost:5080/mcp-senparc-xncf-mcp/sse` with type `sse`.
- SSE servers may use custom paths; the module does not append `/sse`.
- Type `http` uses Streamable HTTP. A pre-save test without a specified type retains automatic HTTP transport detection.
- `stdio` and `websocket` configurations can still be stored, but their connection tests report that the transport is unsupported.
- Saving configuration does not require the remote server to be reachable.
- Connection initialization and tool discovery share a 15-second default timeout. Failed tests remain visibly failed and saved tests persist their timestamp, result and tool count.
- Missing names/addresses, duplicate names, invalid IDs and overlength fields return explicit failure responses.

The existing `Add_MCPEndpoint` migrations create the endpoint table. If the host reports a database schema upgrade requirement, use the normal NCF module/database upgrade workflow; a URL change cannot fix a missing table.

## Regression tests

Run from the repository root:

```bash
dotnet test tools/NcfSimulatedSite/Tests/Senparc.Areas.Admin.Tests/Senparc.Areas.Admin.Tests.csproj \
  --filter 'FullyQualifiedName~McpEndpointManagementTests'

node --test tools/NcfSimulatedSite/Tests/Senparc.Areas.Admin.Tests/Client/McpEndpointManagementTests.js
```

The .NET tests use an isolated SQLite database, real generated API controllers and local HTTP servers. They cover authentication, POST routing, configuration persistence, validation, connection failures, tool/schema discovery, timeout and cancellation. Client tests cover list/save/toggle/delete/test interactions, server errors, malformed responses, duplicate submissions and tool expansion.

To additionally test the running local SSE server:

```bash
MCP_LIVE_TEST_URL=http://localhost:5080/mcp-senparc-xncf-mcp/sse \
  dotnet test tools/NcfSimulatedSite/Tests/Senparc.Areas.Admin.Tests/Senparc.Areas.Admin.Tests.csproj \
  --filter 'FullyQualifiedName~McpEndpointManagementTests'
```
