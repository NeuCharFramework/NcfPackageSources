# Senparc.Ncf.XncfBase

`Senparc.Ncf.XncfBase` is the module runtime foundation for NeuCharFramework. XNCF modules use it to describe themselves, register services, expose functions/pages, manage database contexts, and participate in startup and installation.

## Features

- `IXncfRegister`, `XncfRegisterBase`, registration attributes, and module availability checks.
- Automatic module discovery, startup, installation, update, and database configuration mapping.
- Function-render requests, parameter UI metadata, selection lists, localized descriptions, and MCP hooks.
- XNCF thread builders, middleware contracts, Razor runtime compilation contracts, and version helpers.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.XncfBase" Version="0.26.0-preview3" />
```

## Key API

- `Register.StartNcfEngine(...)` starts the NCF module engine and scans configured assemblies.
- `Register.UseXncfModules(...)` activates discovered modules in an ASP.NET Core application.
- `XncfRegisterManager.IsRegistered(...)` and `CheckXncfAvailable(...)` inspect module state.
- `FunctionRenderCollection.Add(...)`, `GetByRegisterType(...)`, and `GetByModuleUid(...)` manage function metadata.
- `FunctionRequestParameterNormalizer.NormalizeJson(...)` normalizes function request JSON.
- `XncfDatabaseDbContext.Migrate()` and `ResetMigrate()` expose module database lifecycle operations.

An XNCF module should provide a stable UID, register type, display metadata, and explicit dependencies. Treat automatic assembly scanning as a deployment boundary and restrict the scanned paths in production.
