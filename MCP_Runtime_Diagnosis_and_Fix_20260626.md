# Senparc.Web MCP Runtime Diagnosis & Fix (2026-06-26)

## Symptom
- `Senparc.Web` could compile in some cases, but runtime startup/request flow could exit with MCP-related exceptions.

## Root Cause
- MCP dependency graph was inconsistent during MAF migration:
  - Several projects declared `ModelContextProtocol 0.2.0-preview.x`.
  - Actual app restore resolved to `ModelContextProtocol 1.0.0` (`NU1603` fallback), creating mixed-version behavior.
- `Senparc.Xncf.MCP` still contained old startup reflection bootstrap code in `Register.cs` (manual `AIFunctionFactory.Create(...)` + `McpServerTool.Create(...)`), increasing startup fragility.
- MCP registration path lacked isolation; if one module’s MCP registration failed, host startup could be impacted.

## Code Changes
- Unified MCP package versions to `1.0.0` in:
  - `src/Basic/Senparc.Ncf.XncfBase/Senparc.Ncf.XncfBase.csproj`
  - `src/Extensions/Senparc.Xncf.MCP/Senparc.Xncf.MCP.csproj`
  - `src/Extensions/Senparc.Xncf.SenMapic/Senparc.Xncf.SenMapic.csproj`
- Removed obsolete MCP SemanticKernel coupling:
  - Deleted `ModelContextProtocol-SemanticKernel` package reference from `Senparc.Xncf.MCP.csproj`.
  - Removed `ModelContextProtocol.SemanticKernel*` usages from `MyFuctionAppService`.
- Aligned hosted MCP tool usage with MAF-style `HostedMcpServerTool`:
  - `src/Extensions/Senparc.Xncf.MCP/OHS/Local/AppService/MyFuctionAppService.cs`
- Removed fragile manual reflection tool bootstrap from:
  - `src/Extensions/Senparc.Xncf.MCP/Register.cs`
- Hardened MCP server registration and endpoint mapping:
  - `src/Basic/Senparc.Ncf.XncfBase/XncfRegisterBase.cs`
  - Added loadable-type scanning with `ReflectionTypeLoadException` fallback.
  - Added guarded registration/mapping and failure logging to prevent site-wide crash.

## Validation Status In This Environment
- Build/restore verification is blocked by local environment NuGet source/path translation and sandbox network restrictions.
- The key blocker seen in this environment:
  - `NU1301` local source path not found for translated path like `X:/Volumes/.../BuildOutPut`.
- Therefore, final full compile/run verification should be executed in your local dev environment where restore sources are available.

