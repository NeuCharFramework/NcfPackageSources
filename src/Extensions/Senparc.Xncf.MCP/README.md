# MCP Endpoint Management

The admin page is `/Admin/MCP/Index?uid=149d8021-1783-4fc9-97a8-f1a1ba60245b`.
It has two separate lists:

- **MCP servers published by this site**: read-only runtime information, including the owning module, route, supported HTTP transports and copyable endpoint addresses.
- **Client connection configurations**: saved endpoints that clients can call, with filtering, create/edit, enable/disable, delete, connection tests and tool schema inspection.

Published services are not inserted into the configuration table, so the same local service may also have an independently managed client configuration. Changing or deleting that configuration does not enable, disable or unpublish the owning module's server.

The published list joins completed NCF MCP registrations with the host's actual mapped routes. Merely setting `EnableMcpServer` or registering services without successfully mapping a route is not sufficient to appear. Addresses use the current request's scheme, host and deployment path base. Listing a route does not imply anonymous access or Internet reachability; permissions and tools remain controlled by the owning module.

## API registration

All seven management methods in `MCPEndpointAppService` use explicit POST `ApiBind` attributes and admin API authentication. Registering the service with dependency injection alone does **not** publish its methods as HTTP endpoints.

The route prefix is:

```text
/api/Senparc.Xncf.MCP/MCPEndpointAppService/Xncf.MCP_MCPEndpointAppService.
```

Methods: `GetPublishedServers`, `GetAllEndpoints`, `GetEnabledEndpoints`, `SaveEndpoint`, `DeleteEndpoint`, `TestEndpoint`, and `TestConnection`.

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

## Removal of the template database example

Version 0.5.8 removes the DatabaseSample page/menu and the sample Color model, DTO, mapping, service, application service and installation-time seed data. The module keeps its real MCP endpoint configuration model and APIs.

Historical migrations are retained for already deployed databases. The cleanup migrations drop only the sample Color table and leave the `MCPEndpoint` table and its records intact. Use the normal NCF database upgrade workflow before serving requests with the updated model; there is no uninstall/reinstall requirement. A migration downgrade can recreate the empty sample table, but cannot recover deleted sample data.

## Regression tests

Run from the repository root:

```bash
dotnet test tools/NcfSimulatedSite/Tests/Senparc.Areas.Admin.Tests/Senparc.Areas.Admin.Tests.csproj \
  --filter 'FullyQualifiedName~McpEndpointManagementTests|FullyQualifiedName~PublishedMcpServerTests|FullyQualifiedName~McpDatabaseSample'

node --test tools/NcfSimulatedSite/Tests/Senparc.Areas.Admin.Tests/Client/McpEndpointManagementTests.js
```

The .NET tests use isolated databases, real generated API controllers and local HTTP servers. They cover authentication, POST routing, publication discovery, deployment path bases, configuration persistence, validation, connection failures, tool/schema discovery, timeout, cancellation and the sample cleanup migration. Client tests cover the two independent lists, read-only publication display, copy actions, configuration interactions, server errors, malformed responses, duplicate submissions and tool expansion.

To additionally test the running local SSE server:

```bash
MCP_LIVE_TEST_URL=http://localhost:5080/mcp-senparc-xncf-mcp/sse \
  dotnet test tools/NcfSimulatedSite/Tests/Senparc.Areas.Admin.Tests/Senparc.Areas.Admin.Tests.csproj \
  --filter 'FullyQualifiedName~McpEndpointManagementTests'
```
