# Senparc.Ncf.DatabasePlant

`Senparc.Ncf.DatabasePlant` is a development-time database provider bundle for NCF. It references the official NCF database adapters so that migrations and database maintenance can be performed against every supported provider from one project.

## Features

- Makes the SQL Server, SQLite, MySQL, PostgreSQL, Oracle, DaMeng (DM), and in-memory configuration types available together.
- Helps module authors run multi-provider migration and schema checks during development.
- Keeps production dependencies explicit: the bundle can be referenced only for non-Release builds.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.DatabasePlant" Version="0.26.0-preview3" />
```

For a development-only reference:

```xml
<ProjectReference Condition="'$(Configuration)' != 'Release'"
                  Include="..\..\..\Basic\Senparc.Ncf.DatabasePlant\Senparc.Ncf.DatabasePlant.csproj" />
```

## Key API and usage

The package exposes the provider implementations consumed by `Senparc.Ncf.Database.Register.UseNcfDatabase(...)` and by XNCF migration infrastructure. The concrete types include `MySqlDatabaseConfiguration`, `SqlServerDatabaseConfiguration`, `SqliteDatabaseConfiguration`, `PostgreSQLDatabaseConfiguration`, `OracleDatabaseConfiguration`, `DmDatabaseConfiguration`, and `InMemoryDatabaseConfiguration`.

Use the provider-specific package in production when only one database engine is required. Keep this aggregate package in development or migration tooling to avoid shipping unnecessary providers.
