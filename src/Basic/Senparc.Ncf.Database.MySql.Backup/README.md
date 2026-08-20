# Senparc.Ncf.Database.MySql.Backup

`Senparc.Ncf.Database.MySql.Backup` extends the NCF MySQL adapter with backup-aware database configuration behavior.

## Features

- Builds on `Senparc.Ncf.Database.MySql` through `MySqlWithBackupDatabaseConfiguration`.
- Keeps MySQL connection, migration, and schema operations compatible with NCF's database abstraction.
- Exposes the provider-specific backup SQL hook used by NCF maintenance tooling.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.Database.MySql.Backcup" Version="0.26.0-preview3" />
```

The package ID preserves the existing `Backcup` spelling for compatibility; the source directory and type use `Backup`.

## Key API

Use `MySqlWithBackupDatabaseConfiguration` anywhere NCF accepts an `IDatabaseConfiguration`. The inherited `UseDatabase(...)` method configures EF Core and XNCF migration metadata; the overridden backup hook supplies the provider-specific backup operation.

Backups still require database credentials, filesystem permissions, and an operational retention policy supplied by the host application.
