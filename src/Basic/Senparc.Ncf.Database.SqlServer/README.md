# Senparc.Ncf.Database.SqlServer

`Senparc.Ncf.Database.SqlServer` is the SQL Server Entity Framework Core adapter for NCF and XNCF modules.

## Features

- Implements `SqlServerDatabaseConfiguration`.
- Applies SQL Server EF Core options and module-specific migration metadata.
- Fits the standard NCF database factory and `XncfDatabaseDbContext` pipeline.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.Database.SqlServer" Version="0.26.0-preview3" />
```

## Key API

Use `SqlServerDatabaseConfiguration` with `Senparc.Ncf.Database.Register.UseNcfDatabase(...)`. Callers can pass `XncfDatabaseData` so migrations use the correct module assembly and history table.

The host remains responsible for SQL Server connection security, retry policy, encryption settings, and migration execution permissions.
