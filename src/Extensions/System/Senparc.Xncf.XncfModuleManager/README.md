# Senparc.Xncf.XncfModuleManager

`Senparc.Xncf.XncfModuleManager` is the NCF system module that tracks installed XNCF modules and exposes module lifecycle operations.

## Features

- Persists module registration, state, version, and account metadata.
- Supports installation/opening, function discovery, updates, and uninstall-state inspection.
- Provides localized management metadata and multi-database migration contexts.
- Integrates with `Senparc.Ncf.XncfBase` module registration and availability checks.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.XncfModuleManager" Version="0.26.0-preview3" />
```

## Key API

- `XncfStateAppService` exposes module-state application operations.
- `XncfState_InstallAndOpenModuleRequest` requests module installation/opening.
- `XncfState_ShowFunctionsRequest` requests the functions exposed by a module.
- `XncfModuleServiceExtension` provides installed/uninstalled/updated module selection helpers.
- `XncfModuleManagerSenparcEntities` stores module-manager data.

Module installation is code execution and schema change at the application boundary. Restrict it to trusted administrators, verify package provenance, review migrations, and keep module versions compatible with the host.
