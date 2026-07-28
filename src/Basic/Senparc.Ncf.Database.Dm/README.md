# Senparc.Ncf.Database.Dm

`Senparc.Ncf.Database.Dm` adds the DaMeng (DM) Entity Framework Core database provider adapter to NCF's database configuration model.

## Features

- Implements `DmDatabaseConfiguration` on top of NCF's `DatabaseConfigurationBase`.
- Supplies DM-specific EF Core options, migration assembly handling, and database lifecycle hooks.
- Works with XNCF multi-database context and migration discovery.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.Database.Dm" Version="0.26.0-preview3" />
```

## Key API

Use `DmDatabaseConfiguration` with `Senparc.Ncf.Database.Register.UseNcfDatabase(...)` or let the NCF database factory resolve it from the configured `MultipleDatabaseType.Dm` value. Its inherited `UseDatabase(...)` method applies the connection string and XNCF migration metadata.

The DM ADO.NET/EF Core provider remains an application responsibility. Configure its connection string and deployment prerequisites before starting the host.
