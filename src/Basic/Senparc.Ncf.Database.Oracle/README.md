# Senparc.Ncf.Database.Oracle

`Senparc.Ncf.Database.Oracle` integrates Oracle Entity Framework Core support with NCF's database abstraction and XNCF migration pipeline.

## Features

- Provides `OracleDatabaseConfiguration` and the compatibility implementation `OracleDatabaseConfigurationForV11`.
- Applies Oracle-specific EF Core options and migration metadata.
- Allows the Oracle SQL compatibility mode to be selected centrally.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.Database.Oracle" Version="0.26.0-preview3" />
```

## Key API

- `OracleDatabaseConfiguration.SetUseOracleSQLCompatibility(string)` selects a compatibility mode by name.
- `OracleDatabaseConfiguration.SetUseOracleSQLCompatibility(OracleSQLCompatibility)` selects it using the provider enum.
- `UseDatabase(...)` configures a `DbContextOptionsBuilder` with connection and XNCF migration information.

Match the Oracle provider version to the target runtime and validate the selected SQL compatibility mode against the deployed Oracle server.
