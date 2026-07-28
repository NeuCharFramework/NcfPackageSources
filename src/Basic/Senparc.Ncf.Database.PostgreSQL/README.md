# Senparc.Ncf.Database.PostgreSQL

`Senparc.Ncf.Database.PostgreSQL` adds PostgreSQL support to NCF's common Entity Framework Core database configuration model.

## Features

- Implements `PostgreSQLDatabaseConfiguration`.
- Applies PostgreSQL options and XNCF migration assembly/history conventions.
- Uses the same provider-selection and multi-database flow as the other NCF adapters.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.Database.PostgreSQL" Version="0.26.0-preview3" />
```

## Key API

Pass `PostgreSQLDatabaseConfiguration` to `Register.UseNcfDatabase(...)`, or let the configured database factory select it. The inherited `UseDatabase(...)` method is the main entry point for connection strings and module migration metadata.

Configure PostgreSQL naming, extensions, connection pooling, and migration permissions in the host application.
