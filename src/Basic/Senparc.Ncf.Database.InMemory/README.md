# Senparc.Ncf.Database.InMemory

`Senparc.Ncf.Database.InMemory` provides an NCF-compatible in-memory Entity Framework Core database configuration for tests, local development, and lightweight demonstrations.

## Features

- Implements `InMemoryDatabaseConfiguration` for NCF database selection.
- Provides `InMemoryDbContextOptionsBuilderForNcf` and `InMemoryOptionsExtensionForNcf` for NCF-specific options integration.
- Avoids an external database server during unit and integration tests.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.Database.InMemory" Version="0.26.0-preview3" />
```

## Key API

Pass `InMemoryDatabaseConfiguration` to the NCF database registration path, or select the in-memory database type in a test host. The configuration inherits the common `UseDatabase(...)` behavior and participates in the same XNCF `DbContext` setup as relational providers.

In-memory EF Core does not reproduce relational constraints, indexes, SQL translation, or transaction behavior. Use a real provider for migration and production verification.
