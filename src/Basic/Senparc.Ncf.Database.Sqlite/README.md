# Senparc.Ncf.Database.Sqlite

`Senparc.Ncf.Database.Sqlite` provides SQLite database configurations for NCF and XNCF applications.

## Features

- Implements `SqliteDatabaseConfiguration` for file-backed SQLite databases.
- Provides `SqliteMemoryDatabaseConfiguration` for isolated in-memory scenarios.
- Applies SQLite-specific EF Core options while preserving NCF migration conventions.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.Database.Sqlite" Version="0.26.0-preview3" />
```

## Key API

Select `SqliteDatabaseConfiguration` or `SqliteMemoryDatabaseConfiguration` through `Register.UseNcfDatabase(...)`. The common `UseDatabase(...)` method accepts a SQLite connection string and optional XNCF migration data.

Use a stable, writable path for a file database and do not treat SQLite in-memory behavior as equivalent to a multi-process production database.
