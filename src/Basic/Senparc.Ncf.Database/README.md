# Senparc.Ncf.Database

`Senparc.Ncf.Database` is the provider-independent database foundation for NCF and XNCF modules.

## Features

- Defines `DatabaseConfigurationBase`, `IDatabaseConfiguration`, provider selection, and connection configuration contracts.
- Coordinates XNCF `DbContext` pools and multi-database migration metadata.
- Provides database helpers, migration attributes, and common registration/application hooks.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.Database" Version="0.26.0-preview3" />
```

## Key API

- `Register.UseNcfDatabase(IApplicationBuilder, IDatabaseConfiguration)` selects the active provider.
- `DatabaseConfigurationBase.UseDatabase(...)` applies a connection string and module migration configuration.
- `MultipleDatabasePool` and `XncfDatabaseDbContextPool` map XNCF registers to provider-specific contexts.
- `NcfDatabaseHelper.GetCurrentConnectionInfo()` and `TryGetConnectionValue(...)` inspect the active connection configuration.

This package contains the abstraction and orchestration layer. Add exactly the provider package required by the host, then configure connection strings and migrations explicitly.
