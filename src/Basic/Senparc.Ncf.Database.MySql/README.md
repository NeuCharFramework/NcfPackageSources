# Senparc.Ncf.Database.MySql

`Senparc.Ncf.Database.MySql` is the NCF Entity Framework Core adapter for MySQL-compatible databases.

## Features

- Implements `MySqlDatabaseConfiguration` for NCF and XNCF modules.
- Applies MySQL EF Core options and migration assembly/history-table conventions.
- Supports provider selection through the common NCF database factory.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.Database.MySql" Version="0.26.0-preview3" />
```

## Key API

Use `MySqlDatabaseConfiguration` with `Senparc.Ncf.Database.Register.UseNcfDatabase(...)`. Its `UseDatabase(...)` method accepts the connection string and optional `XncfDatabaseData`, so module migrations remain isolated by module and assembly.

Install and configure the compatible MySQL EF Core provider and verify character set, server version, and migration permissions in the host environment.
