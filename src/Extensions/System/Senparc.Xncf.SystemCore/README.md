# Senparc.Xncf.SystemCore

`Senparc.Xncf.SystemCore` is a required NCF system module that provides shared system database entities and configuration persistence.

## Features

- Defines the system `SenparcEntities` context and provider-specific variants.
- Provides `NcfClientDbData` and system configuration repository contracts.
- Registers system-core database services and localized module resources.
- Supports NCF's database pool and migration conventions.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.SystemCore" Version="0.26.0-preview3" />
```

## Key API

- `Register` adds the system core module to the NCF engine.
- `INcfClientDbData` and `NcfClientDbData` expose shared client database data.
- `ISystemConfigRepository` and `SystemConfigRepository` provide configuration persistence.
- `SenparcEntities` is the core system `DbContext`.

This module is part of the host foundation rather than an optional business feature. Keep it version-compatible with `Senparc.Ncf.Core`, `Senparc.Ncf.Database`, and the other system modules.
