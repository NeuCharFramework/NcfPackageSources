# Senparc.Xncf.DatabaseToolkit

`Senparc.Xncf.DatabaseToolkit` is an NCF administration module for inspecting database schemas, querying approved records, updating configuration, exporting SQL, and coordinating backups.

## Features

- Discovers module/entity/table metadata through `DatabaseSchemaMetadataProvider`.
- Provides controlled record queries and table statistics through `DatabaseExecutor`.
- Supports database configuration display/update, backup checks, backup execution, and SQL export.
- Includes multi-database contexts and XNCF function request/response models.
- Exposes optional AI-agent integration helpers for database workflows.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.DatabaseToolkit" Version="0.26.0-preview3" />
```

## Key API

- `DatabaseSchemaMetadataProvider.InitializeAsync()`, `GetSchemasByModule(...)`, `GetTableNames(...)`, and `GetSchemaByTable(...)` build schema metadata.
- `DatabaseExecutor.QueryRecordsAsync(...)` and `GetTableStatisticsAsync(...)` execute the module/table operations.
- `DatabaseOperationAppService.QueryRecords(...)` and `GetStatistics(...)` expose function-level operations.
- `DatabaseConfigAppService.SetConfig(...)` and `ShowDatabaseConfiguration()` manage toolkit settings.
- `DatabaseBackupAppService.IsAutoBackup()`, `Backup(...)`, and `ExportSQL()` expose backup/export operations.
- `DatabaseUpdateAppService.UpdateDatabase()` runs the module's database update workflow.

This module can read and modify database data and can create backup artifacts. Protect every endpoint with administrator authorization, allowlist modules/tables and conditions, redact sensitive results, validate backup paths, and audit operations before production use.
